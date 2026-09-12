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

        var generalExpenses = await LoadGeneralExpensesAsync(request, cancellationToken);

        // Every site that had a cost, or has material sitting on it. A site
        // with none of these is not a row of zeroes, it is a site nothing
        // happened on.
        var projectIds = labour.Keys
            .Concat(materials.Keys)
            .Concat(materialsOnSite.Keys)
            .Concat(manualPay.Keys)
            .Concat(generalExpenses.Keys)
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
                var generalExpenseCost = generalExpenses.GetValueOrDefault(id);

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
                    GeneralExpenseCost = decimal.Round(generalExpenseCost, 2),
                    Total = decimal.Round(l.Cost + materialCost + generalExpenseCost, 2)
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
            TotalGeneralExpenseCost = rows.Sum(r => r.GeneralExpenseCost),
            Total = rows.Sum(r => r.Total)
        };
    }

    /// <summary>
    /// Approved hours per site, priced by the rate in force on the day —
    /// weekend and public-holiday hours at that rate's own premium, when it
    /// sets one; a subcontractor on a daily rate instead earns one flat
    /// amount per day worked, whatever the hours.
    /// </summary>
    /// <remarks>
    /// The covering rate is found per entry with a correlated subquery rather
    /// than loading every rate and matching in memory, so the work stays in
    /// the database where the index is; the subquery returns every price the
    /// rate might carry (hourly, its weekend/holiday premiums, daily) at
    /// once. Which applies is decided afterwards, in memory, because that
    /// decision needs the holiday calendar, a plain <c>DayOfWeek</c> check,
    /// and — for daily rates — grouping entries by employee and day, none of
    /// which translate cleanly into the same query. An entry no rate covers
    /// still contributes its minutes to <c>UnpricedMinutes</c> and nothing to
    /// the cost — reported, not silently free.
    ///
    /// The holiday calendar is per country, because this company runs sites
    /// in more than one at once: a date only counts as a holiday for a shift
    /// whose <see cref="Project.CountryCode"/> matches the calendar row's own.
    /// A project with no country set never gets the holiday rate — nothing to
    /// match it against — whatever the calendar says for any country.
    ///
    /// The day is taken from the shift's start in UTC, exactly as before this
    /// premium existed. A shift beginning after midnight local time therefore
    /// prices — and is weekend/holiday-classified — against the previous day,
    /// which only matters right at a boundary.
    /// </remarks>
    private async Task<Dictionary<Guid, (int Minutes, decimal Cost, int UnpricedMinutes)>>
        LoadLabourAsync(GetProjectCostsQuery request, CancellationToken cancellationToken)
    {
        // Keyed by (country, date): sites in different countries do not share
        // a holiday calendar, so a date only counts as a holiday for a shift
        // whose project's own CountryCode matches the row it came from.
        var holidays = (await _context.PublicHolidays
                .AsNoTracking()
                .Where(h => h.Date >= request.From && h.Date <= request.To)
                .Select(h => new { h.CountryCode, h.Date })
                .ToListAsync(cancellationToken))
            .Select(h => (h.CountryCode, h.Date))
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
                t.EmployeeId,
                ProjectId = t.ProjectId!.Value,
                ProjectCountryCode = t.Project!.CountryCode,
                Day = DateOnly.FromDateTime(t.StartedAt),
                t.WorkType,
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
                        r.RateType,
                        r.HourlyRate,
                        r.WeekendHourlyRate,
                        r.HolidayHourlyRate,
                        r.OvertimeHourlyRate,
                        r.TravelHourlyRate,
                        r.DailyRate
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Hourly-priced entries (and any with no covering rate at all) are
        // priced per entry, as before. A daily-priced entry — the usual shape
        // for a subcontractor — is priced once per employee per calendar day
        // worked, regardless of hours or entry count that day: that is what
        // "a day's pay" means. A day split across more than one site (rare,
        // but not impossible for a subcontractor covering two jobs) puts the
        // whole day's pay on whichever site they logged the most time at,
        // rather than splitting one flat amount nobody agreed to split.
        //
        // WorkType only enters the price for Overtime and Travel — the two
        // tags that mean something a date can never tell you. Weekend and
        // PublicHoliday are deliberately not read here even though a shift
        // could carry that tag too: the calendar already knows which day a
        // shift fell on, correctly, every time, and a hand-picked tag that
        // happened to disagree with the actual date would be the wrong
        // answer, not a more precise one. An Overtime or Travel shift that
        // also falls on a weekend or holiday is priced as Overtime/Travel,
        // not stacked with the weekend/holiday premium — one differently
        // priced hour, not two premiums added together.
        var hourly = priced
            .Where(t => t.Rate is null || t.Rate.RateType == RateType.Hourly)
            .Select(t => new
            {
                t.ProjectId,
                t.Minutes,
                Cost = t.Rate is null
                    ? 0m
                    : (t.WorkType == WorkType.Overtime
                        ? t.Rate.OvertimeHourlyRate ?? t.Rate.HourlyRate
                    : t.WorkType == WorkType.Travel
                        ? t.Rate.TravelHourlyRate ?? t.Rate.HourlyRate
                    : t.ProjectCountryCode != null && holidays.Contains((t.ProjectCountryCode, t.Day))
                        ? t.Rate.HolidayHourlyRate ?? t.Rate.HourlyRate
                    : t.Day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                        ? t.Rate.WeekendHourlyRate ?? t.Rate.HourlyRate
                    : t.Rate.HourlyRate) is { } rate
                        ? rate * t.Minutes / 60m
                        : 0m,
                Unpriced = t.Rate is null ? t.Minutes : 0
            });

        var daily = priced
            .Where(t => t.Rate is not null && t.Rate.RateType == RateType.Daily)
            .GroupBy(t => new { t.EmployeeId, t.Day })
            .Select(g => new
            {
                ProjectId = g.OrderByDescending(x => x.Minutes).First().ProjectId,
                Minutes = g.Sum(x => x.Minutes),
                Cost = g.First().Rate!.DailyRate ?? 0m,
                Unpriced = 0
            });

        return hourly.Concat(daily)
            .GroupBy(t => t.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Minutes: g.Sum(t => t.Minutes),
                    Cost: g.Sum(t => t.Cost),
                    UnpricedMinutes: g.Sum(t => t.Unpriced)));
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

    /// <summary>
    /// General expenses (<c>GeneralExpense</c>) attributed to a site over the
    /// period — housing, bookkeeping, damage, and the rest of what has no
    /// other ledger. Unlike <see cref="LoadFinanceEntriesAsync"/>'s figure,
    /// this one is safe to fold straight into the total: it has no other
    /// source it could be double-counting against.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> LoadGeneralExpensesAsync(
        GetProjectCostsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _context.GeneralExpenses
            .AsNoTracking()
            .Where(e => e.ProjectId != null)
            .Where(e => request.ProjectId == null || e.ProjectId == request.ProjectId)
            .Where(e => e.OccurredOn >= request.From && e.OccurredOn <= request.To)
            .GroupBy(e => e.ProjectId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                Amount = g.Sum(e => e.Amount)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProjectId, r => r.Amount);
    }
}
