using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetProjectCosts;

/// <summary>
/// What each site cost over a period: labour plus material.
/// </summary>
/// <remarks>
/// Only approved hours count. Unreviewed hours are a claim, not a cost, and a
/// total that moved every time somebody clocked out would be unusable for
/// pricing the next job.
///
/// Vehicles are deliberately absent. A van is not posted to a site the way a
/// person is, so attributing its diesel to one project would mean inventing an
/// allocation the data does not support. The fleet has its own report.
/// </remarks>
public record GetProjectCostsQuery : IRequest<ProjectCostReportDto>
{
    /// <summary>Widest period the report will cover. Two years of monthly comparison.</summary>
    public const int MaxDays = 732;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>Narrows the report to one site.</summary>
    public Guid? ProjectId { get; init; }
}

public class GetProjectCostsQueryValidator : AbstractValidator<GetProjectCostsQuery>
{
    public GetProjectCostsQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetProjectCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetProjectCostsQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetProjectCostsQueryHandler
    : IRequestHandler<GetProjectCostsQuery, ProjectCostReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectCostsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectCostReportDto> Handle(
        GetProjectCostsQuery request,
        CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role;

        if (!CostRules.CanSeeSpending(role))
        {
            throw new ForbiddenAccessException("You may not see cost reports.");
        }

        var includesLabour = CostRules.CanSeeLabourCost(role);
        var from = request.From;
        var to = request.To;

        var labour = includesLabour
            ? await LoadLabourAsync(request, cancellationToken)
            : [];

        var materials = await LoadMaterialsAsync(request, cancellationToken);
        var materialsOnSite = await LoadMaterialsOnSiteAsync(request, cancellationToken);

        // Same visibility rule as clocked labour: a manual pay figure is
        // payroll, and a role that cannot see one should not see the other.
        var manualPay = includesLabour
            ? await LoadFinanceEntriesAsync(request, cancellationToken)
            : [];

        // Every site that had a cost, or has material sitting on it. A site
        // with none of these is not a row of zeroes, it is a site nothing
        // happened on.
        var projectIds = labour.Keys
            .Concat(materials.Keys)
            .Concat(materialsOnSite.Keys)
            .Concat(manualPay.Keys)
            .ToHashSet();

        var names = await _context.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var rows = projectIds
            .Select(id =>
            {
                var l = labour.GetValueOrDefault(id);
                var materialCost = materials.GetValueOrDefault(id);
                var onSiteValue = materialsOnSite.GetValueOrDefault(id);
                var manualPayAmount = manualPay.GetValueOrDefault(id);

                return new ProjectCostRowDto
                {
                    ProjectId = id,
                    ProjectName = names.GetValueOrDefault(id) ?? string.Empty,
                    LabourMinutes = l.Minutes,
                    LabourCost = decimal.Round(l.Cost, 2),
                    UnpricedMinutes = l.UnpricedMinutes,
                    MaterialCost = decimal.Round(materialCost, 2),
                    MaterialsOnSiteValue = decimal.Round(onSiteValue, 2),
                    ManualPayAmount = decimal.Round(manualPayAmount, 2),
                    Total = decimal.Round(l.Cost + materialCost, 2)
                };
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.ProjectName)
            .ToList();

        return new ProjectCostReportDto
        {
            From = from,
            To = to,
            IncludesLabour = includesLabour,
            Rows = rows,
            TotalLabourCost = rows.Sum(r => r.LabourCost),
            TotalMaterialCost = rows.Sum(r => r.MaterialCost),
            TotalMaterialsOnSiteValue = rows.Sum(r => r.MaterialsOnSiteValue),
            TotalManualPayAmount = rows.Sum(r => r.ManualPayAmount),
            Total = rows.Sum(r => r.Total)
        };
    }

    /// <summary>
    /// Approved hours per site, priced by the rate in force on the day —
    /// weekend and public-holiday hours at that rate's own premium, when it
    /// sets one.
    /// </summary>
    /// <remarks>
    /// The covering rate is found per entry with a correlated subquery rather
    /// than loading every rate and matching in memory, so the work stays in
    /// the database where the index is; the subquery now returns all three
    /// prices at once instead of just the base hourly rate.
    /// Which of the three applies is decided afterwards, in memory, because
    /// that decision needs the holiday calendar and a plain <c>DayOfWeek</c>
    /// check — neither translates cleanly into the same query. An entry no
    /// rate covers still contributes its minutes to <c>UnpricedMinutes</c> and
    /// nothing to the cost — reported, not silently free.
    ///
    /// The day is taken from the shift's start in UTC, exactly as before this
    /// premium existed. A shift beginning after midnight local time therefore
    /// prices — and is weekend/holiday-classified — against the previous day,
    /// which only matters right at a boundary.
    /// </remarks>
    private async Task<Dictionary<Guid, (int Minutes, decimal Cost, int UnpricedMinutes)>>
        LoadLabourAsync(GetProjectCostsQuery request, CancellationToken cancellationToken)
    {
        var holidays = (await _context.PublicHolidays
                .AsNoTracking()
                .Where(h => h.Date >= request.From && h.Date <= request.To)
                .Select(h => h.Date)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var priced = await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Status == TimeEntryStatus.Approved
                && t.ProjectId != null
                && t.EndedAt != null)
            .Where(t => request.ProjectId == null || t.ProjectId == request.ProjectId)
            .Where(t => DateOnly.FromDateTime(t.StartedAt) >= request.From
                && DateOnly.FromDateTime(t.StartedAt) <= request.To)
            .Select(t => new
            {
                ProjectId = t.ProjectId!.Value,
                Day = DateOnly.FromDateTime(t.StartedAt),
                // Npgsql turns the subtraction into an interval and
                // TotalMinutes into the epoch extraction; the same shape the
                // timesheet summary already uses.
                Minutes = (int)((t.EndedAt!.Value - t.StartedAt).TotalMinutes
                    - t.BreakMinutes),
                Rate = _context.EmployeeRates
                    .Where(r => r.EmployeeId == t.EmployeeId
                        && r.StartDate <= DateOnly.FromDateTime(t.StartedAt)
                        && (r.EndDate == null || r.EndDate >= DateOnly.FromDateTime(t.StartedAt)))
                    .Select(r => new
                    {
                        r.HourlyRate,
                        r.WeekendHourlyRate,
                        r.HolidayHourlyRate
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var withEffectiveRate = priced.Select(t => new
        {
            t.ProjectId,
            t.Minutes,
            EffectiveRate = t.Rate is null
                ? (decimal?)null
                : holidays.Contains(t.Day)
                    ? t.Rate.HolidayHourlyRate ?? t.Rate.HourlyRate
                : t.Day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                    ? t.Rate.WeekendHourlyRate ?? t.Rate.HourlyRate
                : t.Rate.HourlyRate
        });

        return withEffectiveRate
            .GroupBy(t => t.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Minutes: g.Sum(t => t.Minutes),
                    Cost: g.Sum(t => t.EffectiveRate is { } rate ? rate * t.Minutes / 60m : 0m),
                    UnpricedMinutes: g.Where(t => t.EffectiveRate is null).Sum(t => t.Minutes)));
    }

    /// <summary>
    /// What each site consumed, at the value snapshotted when it was issued.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> LoadMaterialsAsync(
        GetProjectCostsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.Kind == MaterialMovementKind.Out
                && m.ProjectId != null
                && m.UnitPrice != null)
            .Where(m => request.ProjectId == null || m.ProjectId == request.ProjectId)
            .Where(m => m.OccurredOn >= request.From && m.OccurredOn <= request.To)
            .GroupBy(m => m.ProjectId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                Cost = g.Sum(m => m.UnitPrice!.Value * m.Quantity)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProjectId, r => r.Cost);
    }

    /// <summary>
    /// What each site is currently holding, priced at each material's own
    /// reference price — not the same question as <see cref="LoadMaterialsAsync"/>,
    /// which prices what was actually issued.
    /// </summary>
    /// <remarks>
    /// A snapshot of the material's own <c>ProjectId</c>, so it is deliberately
    /// not filtered by <see cref="GetProjectCostsQuery.From"/>/<see cref="GetProjectCostsQuery.To"/>
    /// — on-hand stock has no period, only a right-now.
    /// </remarks>
    private async Task<Dictionary<Guid, decimal>> LoadMaterialsOnSiteAsync(
        GetProjectCostsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _context.Materials
            .AsNoTracking()
            .Where(m => m.ProjectId != null && m.UnitPrice != null)
            .Where(m => request.ProjectId == null || m.ProjectId == request.ProjectId)
            .GroupBy(m => m.ProjectId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                Value = g.Sum(m => m.UnitPrice!.Value * m.Quantity)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProjectId, r => r.Value);
    }

    /// <summary>
    /// Manually entered pay (<c>FinanceEntry</c>) attributed to a site over
    /// the period — see the remarks on <see cref="ProjectCostRowDto.ManualPayAmount"/>
    /// for why this is kept separate from <see cref="LoadLabourAsync"/> rather
    /// than added to it.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> LoadFinanceEntriesAsync(
        GetProjectCostsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _context.FinanceEntries
            .AsNoTracking()
            .Where(f => f.ProjectId != null)
            .Where(f => request.ProjectId == null || f.ProjectId == request.ProjectId)
            .Where(f => f.OccurredOn >= request.From && f.OccurredOn <= request.To)
            .GroupBy(f => f.ProjectId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                Amount = g.Sum(f => f.Amount)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProjectId, r => r.Amount);
    }
}
