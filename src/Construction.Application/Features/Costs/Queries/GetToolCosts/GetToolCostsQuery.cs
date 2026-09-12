using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetToolCosts;

/// <summary>What the tool fleet cost over a period.</summary>
public record GetToolCostsQuery : IRequest<ToolCostReportDto>
{
    public const int MaxDays = 732;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public Guid? ToolId { get; init; }
}

public class GetToolCostsQueryValidator : AbstractValidator<GetToolCostsQuery>
{
    public GetToolCostsQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetToolCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetToolCostsQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetToolCostsQueryHandler
    : IRequestHandler<GetToolCostsQuery, ToolCostReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetToolCostsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolCostReportDto> Handle(
        GetToolCostsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see cost reports.");
        }

        var grouped = await _context.ToolExpenses
            .AsNoTracking()
            .Where(e => request.ToolId == null || e.ToolId == request.ToolId)
            .Where(e => e.OccurredOn >= request.From && e.OccurredOn <= request.To)
            .GroupBy(e => new { e.ToolId, e.Tool.Name })
            .Select(g => new
            {
                g.Key.ToolId,
                g.Key.Name,
                RepairCost = g.Where(e => e.Kind == ToolExpenseKind.Repair)
                    .Sum(e => (decimal?)e.Amount) ?? 0m,
                MaintenanceCost = g.Where(e => e.Kind == ToolExpenseKind.Maintenance)
                    .Sum(e => (decimal?)e.Amount) ?? 0m,
                TotalCost = g.Sum(e => (decimal?)e.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        // Rental/lease cost is a separate source — a tool can carry one with
        // no ToolExpense rows at all in the period, so it needs its own tool
        // lookup rather than riding along with the group above.
        var rentalRates = await _context.ToolRentalRates
            .AsNoTracking()
            .Where(r => request.ToolId == null || r.ToolId == request.ToolId)
            .Where(r => r.StartDate <= request.To && (r.EndDate == null || r.EndDate >= request.From))
            .Select(r => new
            {
                r.ToolId,
                r.Tool.Name,
                r.StartDate,
                r.EndDate,
                r.MonthlyAmount
            })
            .ToListAsync(cancellationToken);

        // A 30-day month is a deliberate approximation, the same one a flat
        // "per day" reading of a monthly figure always is — the point is a
        // consistent number to compare period over period, not an invoice
        // reconciliation.
        var rentalByTool = rentalRates
            .GroupBy(r => new { r.ToolId, r.Name })
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r =>
                {
                    var start = r.StartDate > request.From ? r.StartDate : request.From;
                    var end = r.EndDate is { } e && e < request.To ? e : request.To;
                    var days = end.DayNumber - start.DayNumber + 1;
                    return days > 0 ? days * (r.MonthlyAmount / 30m) : 0m;
                }));

        // Revenue-out is a discrete-row source, not a dated chain: a loan is
        // priced across the overlap with the period, an open one only up to
        // today (mirrors GetToolRentalsOutSummaryQueryHandler's TotalValue).
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var rentalsOut = await _context.ToolRentalsOut
            .AsNoTracking()
            .Where(r => request.ToolId == null || r.ToolId == request.ToolId)
            .Where(r => r.StartDate <= request.To && (r.EndDate == null || r.EndDate >= request.From))
            .Select(r => new
            {
                r.ToolId,
                r.Tool.Name,
                r.StartDate,
                r.EndDate,
                r.DailyRate
            })
            .ToListAsync(cancellationToken);

        var revenueByTool = rentalsOut
            .GroupBy(r => new { r.ToolId, r.Name })
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r =>
                {
                    var start = r.StartDate > request.From ? r.StartDate : request.From;
                    var end = r.EndDate ?? today;
                    end = end < request.To ? end : request.To;
                    var days = end.DayNumber - start.DayNumber + 1;
                    return days > 0 ? days * r.DailyRate : 0m;
                }));

        var toolNames = grouped
            .Select(t => (t.ToolId, t.Name))
            .Concat(rentalByTool.Keys.Select(k => (k.ToolId, k.Name)))
            .Concat(revenueByTool.Keys.Select(k => (k.ToolId, k.Name)))
            .GroupBy(t => t.ToolId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var toolIds = toolNames.Keys.ToHashSet();
        var expensesByTool = grouped.ToDictionary(t => t.ToolId);

        var rows = toolIds
            .Select(toolId =>
            {
                var t = expensesByTool.GetValueOrDefault(toolId);
                var rentalCost = rentalByTool
                    .Where(kv => kv.Key.ToolId == toolId)
                    .Select(kv => kv.Value)
                    .FirstOrDefault();
                var revenue = revenueByTool
                    .Where(kv => kv.Key.ToolId == toolId)
                    .Select(kv => kv.Value)
                    .FirstOrDefault();

                var repairCost = t?.RepairCost ?? 0m;
                var maintenanceCost = t?.MaintenanceCost ?? 0m;
                var totalCost = t?.TotalCost ?? 0m;
                var total = totalCost + rentalCost;

                return new ToolCostRowDto
                {
                    ToolId = toolId,
                    ToolName = toolNames[toolId],
                    RepairCost = decimal.Round(repairCost, 2),
                    MaintenanceCost = decimal.Round(maintenanceCost, 2),
                    OtherCost = decimal.Round(totalCost - repairCost - maintenanceCost, 2),
                    RentalCost = decimal.Round(rentalCost, 2),
                    Total = decimal.Round(total, 2),
                    Revenue = decimal.Round(revenue, 2),
                    Profit = decimal.Round(revenue - total, 2)
                };
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.ToolName)
            .ToList();

        return new ToolCostReportDto
        {
            From = request.From,
            To = request.To,
            Rows = rows,
            Total = rows.Sum(r => r.Total),
            TotalRentalCost = rows.Sum(r => r.RentalCost),
            TotalRevenue = rows.Sum(r => r.Revenue),
            TotalProfit = rows.Sum(r => r.Profit)
        };
    }
}
