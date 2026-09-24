using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Queries.GetCompanyCosts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

public class FinanceShareDto
{
    /// <summary>"Labour", "ManualPay", "Material", "GeneralExpenses", "Accommodation", "Vehicles" or "Tools".</summary>
    public string Kind { get; init; } = null!;

    /// <summary>This kind's share of all spending, in percent.</summary>
    public decimal SharePercent { get; init; }
}

/// <summary>
/// What the company's money did over a period, without a single amount in it.
/// </summary>
/// <remarks>
/// For an account granted <see cref="Domain.Enums.FinanceAccess.StatisticsOnly"/>: how income, spending and
/// profit moved against the period before, and what the spending was on, all
/// as percentages. Nothing here — no amount, no margin (which beside a visible
/// income would give the profit away), no share beside a visible total — can be
/// turned back into money. See <c>docs/WIDGETI_TROSKOVA_PLAN.md</c>, O-7.
/// </remarks>
public class FinanceStatisticsDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>False when the caller's role may not see pay rates, so labour and pay are left out of spending.</summary>
    public bool IncludesLabour { get; init; }

    /// <summary>Change of income against the period before, in percent. Null when there was none before to compare with.</summary>
    public decimal? RevenueChangePercent { get; init; }

    public decimal? ExpenseChangePercent { get; init; }

    public decimal? ProfitChangePercent { get; init; }

    /// <summary>What spending was on. Empty when nothing was spent.</summary>
    public IReadOnlyList<FinanceShareDto> Shares { get; init; } = [];
}

public record GetFinanceStatisticsQuery : IRequest<FinanceStatisticsDto>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }
}

public class GetFinanceStatisticsQueryValidator : AbstractValidator<GetFinanceStatisticsQuery>
{
    public GetFinanceStatisticsQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From).WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetCompanyCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetCompanyCostsQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetFinanceStatisticsQueryHandler : IRequestHandler<GetFinanceStatisticsQuery, FinanceStatisticsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public GetFinanceStatisticsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<FinanceStatisticsDto> Handle(GetFinanceStatisticsQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureStatisticsAsync(_context, _currentUserService, cancellationToken);

        var days = request.To.DayNumber - request.From.DayNumber + 1;
        var previousTo = request.From.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(days - 1));

        // The right that lets this be asked is not the right to see amounts, so the cost reports'
        // own finance check is stepped over; what comes back is turned into percentages here
        // and the amounts go no further. The role rules of the report still apply.
        var current = await _sender.Send(
            new GetCompanyCostsQuery { From = request.From, To = request.To, SkipFinanceCheck = true },
            cancellationToken);
        var previous = await _sender.Send(
            new GetCompanyCostsQuery { From = previousFrom, To = previousTo, SkipFinanceCheck = true },
            cancellationToken);

        var revenue = await RevenueAsync(request.From, request.To, cancellationToken);
        var previousRevenue = await RevenueAsync(previousFrom, previousTo, cancellationToken);

        var profit = revenue - current.Total;
        var previousProfit = previousRevenue - previous.Total;

        return new FinanceStatisticsDto
        {
            From = request.From,
            To = request.To,
            IncludesLabour = current.IncludesLabour,
            RevenueChangePercent = Change(revenue, previousRevenue),
            ExpenseChangePercent = Change(current.Total, previous.Total),
            ProfitChangePercent = Change(profit, previousProfit),
            Shares = Shares(current),
        };
    }

    private async Task<decimal> RevenueAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var project = await _context.ProjectRevenues.AsNoTracking()
            .Where(r => r.OccurredOn >= from && r.OccurredOn <= to)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var other = await _context.CompanyRevenues.AsNoTracking()
            .Where(r => r.OccurredOn >= from && r.OccurredOn <= to)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        return project + other;
    }

    /// <summary>Relative to the size of the earlier figure even when it was a loss; null rather than infinite when it was nothing.</summary>
    internal static decimal? Change(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((current - previous) / Math.Abs(previous) * 100m, 1);

    private static List<FinanceShareDto> Shares(CompanyCostsDto costs)
    {
        if (costs.Total <= 0)
        {
            return [];
        }

        (string Kind, decimal Amount)[] parts =
        [
            ("Labour", costs.Labour),
            ("ManualPay", costs.ManualPay),
            ("Material", costs.Material),
            ("GeneralExpenses", costs.GeneralExpenses),
            ("Accommodation", costs.Accommodation),
            ("Vehicles", costs.Vehicles),
            ("Tools", costs.Tools),
        ];

        return parts
            .Where(p => p.Amount > 0)
            .Select(p => new FinanceShareDto { Kind = p.Kind, SharePercent = Math.Round(p.Amount / costs.Total * 100m, 1) })
            .OrderByDescending(s => s.SharePercent)
            .ToList();
    }
}
