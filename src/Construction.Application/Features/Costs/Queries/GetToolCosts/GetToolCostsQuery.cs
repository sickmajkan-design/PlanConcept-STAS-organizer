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

    public GetToolCostsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        var rows = grouped
            .Select(t => new ToolCostRowDto
            {
                ToolId = t.ToolId,
                ToolName = t.Name,
                RepairCost = decimal.Round(t.RepairCost, 2),
                MaintenanceCost = decimal.Round(t.MaintenanceCost, 2),
                OtherCost = decimal.Round(t.TotalCost - t.RepairCost - t.MaintenanceCost, 2),
                Total = decimal.Round(t.TotalCost, 2)
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.ToolName)
            .ToList();

        return new ToolCostReportDto
        {
            From = request.From,
            To = request.To,
            Rows = rows,
            Total = rows.Sum(r => r.Total)
        };
    }
}
