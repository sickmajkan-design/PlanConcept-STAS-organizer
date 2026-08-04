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

        // Every site that had either kind of cost. A site with neither is not
        // a row of zeroes, it is a site nothing happened on.
        var projectIds = labour.Keys.Concat(materials.Keys).ToHashSet();

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

                return new ProjectCostRowDto
                {
                    ProjectId = id,
                    ProjectName = names.GetValueOrDefault(id) ?? string.Empty,
                    LabourMinutes = l.Minutes,
                    LabourCost = decimal.Round(l.Cost, 2),
                    UnpricedMinutes = l.UnpricedMinutes,
                    MaterialCost = decimal.Round(materialCost, 2),
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
            Total = rows.Sum(r => r.Total)
        };
    }

    /// <summary>
    /// Approved hours per site, priced by the rate in force on the day.
    /// </summary>
    /// <remarks>
    /// The rate is found per entry with a correlated subquery rather than
    /// loading every rate and matching in memory, so the work stays in the
    /// database where the index is. An entry no rate covers contributes its
    /// minutes to <c>UnpricedMinutes</c> and nothing to the cost — reported,
    /// not silently free.
    ///
    /// The day is taken from the shift's start in UTC. A shift beginning
    /// after midnight local time therefore prices against the previous day,
    /// which only matters when a rate changes on exactly that date.
    /// </remarks>
    private async Task<Dictionary<Guid, (int Minutes, decimal Cost, int UnpricedMinutes)>>
        LoadLabourAsync(GetProjectCostsQuery request, CancellationToken cancellationToken)
    {
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
                // Npgsql turns the subtraction into an interval and
                // TotalMinutes into the epoch extraction; the same shape the
                // timesheet summary already uses.
                Minutes = (int)((t.EndedAt!.Value - t.StartedAt).TotalMinutes
                    - t.BreakMinutes),
                HourlyRate = _context.EmployeeRates
                    .Where(r => r.EmployeeId == t.EmployeeId
                        && r.StartDate <= DateOnly.FromDateTime(t.StartedAt)
                        && (r.EndDate == null || r.EndDate >= DateOnly.FromDateTime(t.StartedAt)))
                    .Select(r => (decimal?)r.HourlyRate)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return priced
            .GroupBy(t => t.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Minutes: g.Sum(t => t.Minutes),
                    Cost: g.Sum(t => t.HourlyRate is { } rate ? rate * t.Minutes / 60m : 0m),
                    UnpricedMinutes: g.Where(t => t.HourlyRate is null).Sum(t => t.Minutes)));
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
}
