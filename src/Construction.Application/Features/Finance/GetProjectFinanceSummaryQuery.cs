using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

public class FinanceMoneyDto
{
    public decimal Revenue { get; init; }

    public decimal Expense { get; init; }

    public decimal Profit { get; init; }

    /// <summary>Profit as a share of revenue, in percent. Null when nothing came in.</summary>
    public decimal? MarginPercent { get; init; }
}

public class ProjectFinanceSummaryDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public decimal? ContractValue { get; init; }

    public decimal? Budget { get; init; }

    /// <summary>False when the caller may not see pay rates, so labour and pay are zero.</summary>
    public bool IncludesLabour { get; init; }

    /// <summary>The requested period.</summary>
    public FinanceMoneyDto Period { get; init; } = null!;

    /// <summary>Everything from the start of the project to today — what a budget is measured against.</summary>
    public FinanceMoneyDto ToDate { get; init; } = null!;

    /// <summary>The first day <see cref="ToDate"/> covers.</summary>
    public DateOnly ToDateFrom { get; init; }

    /// <summary>Spending to date as a share of the budget, in percent. Null when there is no (or a zero) budget.</summary>
    public decimal? BudgetUsedPercent { get; init; }

    /// <summary>Revenue to date as a share of the contract value, in percent. Null when there is none.</summary>
    public decimal? ContractCollectedPercent { get; init; }
}

/// <summary>One project's income, spending and profit — in a period, and from its start to today.</summary>
/// <remarks>
/// "To date" starts on the earliest of the project's start date and the day it
/// was created, less the most a cost can be backdated, so no cost recorded for
/// it falls before it. The cost report will not cover more than
/// <see cref="GetProjectCostsQuery.MaxDays"/> days at once, so a long project is
/// summed window by window; nothing overlaps, so nothing is counted twice.
/// </remarks>
public record GetProjectFinanceSummaryQuery : IRequest<ProjectFinanceSummaryDto>
{
    public Guid ProjectId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }
}

public class GetProjectFinanceSummaryQueryValidator : AbstractValidator<GetProjectFinanceSummaryQuery>
{
    public GetProjectFinanceSummaryQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From).WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetProjectCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetProjectCostsQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetProjectFinanceSummaryQueryHandler
    : IRequestHandler<GetProjectFinanceSummaryQuery, ProjectFinanceSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISender _sender;

    public GetProjectFinanceSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _sender = sender;
    }

    public async Task<ProjectFinanceSummaryDto> Handle(
        GetProjectFinanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new { p.Id, p.Name, p.ContractValue, p.Budget, p.StartDate, p.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var created = DateOnly.FromDateTime(project.CreatedAt);
        var earliest = project.StartDate is { } start && start < created ? start : created;
        var toDateFrom = earliest.AddDays(-CostRules.MaxBackdatingDays);

        // The window may not end before the period asked for does.
        var toDateTo = request.To > today ? request.To : today;
        if (request.From < toDateFrom)
        {
            toDateFrom = request.From;
        }

        var (periodExpense, includesLabour) = await ExpenseAsync(project.Id, request.From, request.To, cancellationToken);
        var periodRevenue = await RevenueAsync(project.Id, request.From, request.To, cancellationToken);

        var toDateExpense = 0m;
        var windowStart = toDateFrom;

        while (windowStart <= toDateTo)
        {
            var windowEnd = windowStart.AddDays(GetProjectCostsQuery.MaxDays - 1);
            if (windowEnd > toDateTo)
            {
                windowEnd = toDateTo;
            }

            toDateExpense += (await ExpenseAsync(project.Id, windowStart, windowEnd, cancellationToken)).Expense;
            windowStart = windowEnd.AddDays(1);
        }

        var toDateRevenue = await RevenueAsync(project.Id, toDateFrom, toDateTo, cancellationToken);

        return new ProjectFinanceSummaryDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ContractValue = project.ContractValue,
            Budget = project.Budget,
            IncludesLabour = includesLabour,
            Period = Money(periodRevenue, periodExpense),
            ToDate = Money(toDateRevenue, toDateExpense),
            ToDateFrom = toDateFrom,
            BudgetUsedPercent = project.Budget is > 0
                ? Math.Round(toDateExpense / project.Budget.Value * 100m, 1)
                : null,
            ContractCollectedPercent = project.ContractValue is > 0
                ? Math.Round(toDateRevenue / project.ContractValue.Value * 100m, 1)
                : null,
        };
    }

    private async Task<(decimal Expense, bool IncludesLabour)> ExpenseAsync(
        Guid projectId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var report = await _sender.Send(
            new GetProjectCostsQuery { From = from, To = to, ProjectId = projectId },
            cancellationToken);

        return (report.Rows.FirstOrDefault(r => r.ProjectId == projectId)?.Total ?? 0m, report.IncludesLabour);
    }

    private async Task<decimal> RevenueAsync(Guid projectId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        await _context.ProjectRevenues
            .AsNoTracking()
            .Where(r => r.ProjectId == projectId && r.OccurredOn >= from && r.OccurredOn <= to)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

    private static FinanceMoneyDto Money(decimal revenue, decimal expense) => new()
    {
        Revenue = revenue,
        Expense = expense,
        Profit = revenue - expense,
        MarginPercent = revenue > 0 ? Math.Round((revenue - expense) / revenue * 100m, 1) : null,
    };
}
