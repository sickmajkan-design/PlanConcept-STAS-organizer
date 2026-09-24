using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Queries.GetCompanyCosts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

/// <summary>How a period is cut into the bars of a chart.</summary>
public enum FinanceGranularity
{
    Day = 1,

    /// <summary>Weeks start on Monday; the first and last are clipped to the period.</summary>
    Week = 2,

    Month = 3,
}

public class FinanceBucketDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public decimal Expense { get; init; }

    /// <summary>Project income plus everything else the company received.</summary>
    public decimal Revenue { get; init; }

    public decimal RevenueProject { get; init; }

    public decimal RevenueOther { get; init; }

    public decimal Profit { get; init; }
}

public class FinanceTotalsDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public decimal Expense { get; init; }

    public decimal Revenue { get; init; }

    public decimal RevenueProject { get; init; }

    public decimal RevenueOther { get; init; }

    public decimal Profit { get; init; }

    /// <summary>Profit as a share of revenue, in percent. Null when there is no revenue to be a share of.</summary>
    public decimal? MarginPercent { get; init; }
}

public class FinanceSeriesDto
{
    public FinanceGranularity Granularity { get; init; }

    /// <summary>False when the caller may not see pay rates, so labour is zero in every bar.</summary>
    public bool IncludesLabour { get; init; }

    public IReadOnlyList<FinanceBucketDto> Buckets { get; init; } = [];

    /// <summary>The sum of <see cref="Buckets"/>, so a bar chart and a headline figure can never disagree.</summary>
    public FinanceTotalsDto Totals { get; init; } = null!;

    /// <summary>The period of the same length that ends the day before this one starts.</summary>
    public FinanceTotalsDto Previous { get; init; } = null!;
}

/// <summary>Income, spending and profit over a period, cut into days, weeks or months.</summary>
/// <remarks>
/// Computed here rather than by the client so that one request answers the
/// question whatever the granularity — the dashboard used to make one call per
/// month, which does not scale to one per day. The spending in each bar is the
/// company cost report for that bar's own dates, so it can never disagree with
/// the cost report.
/// </remarks>
public record GetFinanceSeriesQuery : IRequest<FinanceSeriesDto>
{
    public const int MaxDays = GetCompanyCostsQuery.MaxDays;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public FinanceGranularity Granularity { get; init; } = FinanceGranularity.Day;
}

public class GetFinanceSeriesQueryValidator : AbstractValidator<GetFinanceSeriesQuery>
{
    public GetFinanceSeriesQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From).WithMessage("The end of the period must not be before its start.")
            .When(x => x.From != default);

        RuleFor(x => x.Granularity).IsInEnum();

        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber + 1 <= GetFinanceSeriesQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetFinanceSeriesQuery.MaxDays} days.")
            .When(x => x.From != default && x.To >= x.From);

        RuleFor(x => x)
            .Must(x => FinanceBuckets.Build(x.From, x.To, x.Granularity).Count <= FinanceBuckets.MaxBuckets(x.Granularity))
            .WithMessage("That period has too many bars at this granularity — pick weeks or months.")
            .When(x => x.From != default && x.To >= x.From && x.To.DayNumber - x.From.DayNumber + 1 <= GetFinanceSeriesQuery.MaxDays);
    }
}

/// <summary>Cutting a period into bars.</summary>
public static class FinanceBuckets
{
    public static int MaxBuckets(FinanceGranularity granularity) => granularity switch
    {
        FinanceGranularity.Day => 93,
        FinanceGranularity.Week => 110,
        _ => 30,
    };

    public static List<(DateOnly From, DateOnly To)> Build(DateOnly from, DateOnly to, FinanceGranularity granularity)
    {
        var buckets = new List<(DateOnly, DateOnly)>();
        var start = from;

        while (start <= to)
        {
            var naturalEnd = granularity switch
            {
                FinanceGranularity.Day => start,
                // Days until the coming Sunday, so the week ends on it.
                FinanceGranularity.Week => start.AddDays((7 - (int)start.DayOfWeek) % 7),
                _ => new DateOnly(start.Year, start.Month, DateTime.DaysInMonth(start.Year, start.Month)),
            };

            var end = naturalEnd < to ? naturalEnd : to;
            buckets.Add((start, end));
            start = end.AddDays(1);
        }

        return buckets;
    }
}

public class GetFinanceSeriesQueryHandler : IRequestHandler<GetFinanceSeriesQuery, FinanceSeriesDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public GetFinanceSeriesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<FinanceSeriesDto> Handle(GetFinanceSeriesQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var ranges = FinanceBuckets.Build(request.From, request.To, request.Granularity);

        var projectByDay = await SumByDayAsync(
            _context.ProjectRevenues.AsNoTracking()
                .Where(r => r.OccurredOn >= request.From && r.OccurredOn <= request.To)
                .GroupBy(r => r.OccurredOn)
                .Select(g => new DayAmount(g.Key, g.Sum(r => r.Amount))),
            cancellationToken);

        var otherByDay = await SumByDayAsync(
            _context.CompanyRevenues.AsNoTracking()
                .Where(r => r.OccurredOn >= request.From && r.OccurredOn <= request.To)
                .GroupBy(r => r.OccurredOn)
                .Select(g => new DayAmount(g.Key, g.Sum(r => r.Amount))),
            cancellationToken);

        var includesLabour = true;
        var buckets = new List<FinanceBucketDto>(ranges.Count);

        // One at a time: they share this request's DbContext.
        foreach (var (from, to) in ranges)
        {
            var costs = await _sender.Send(new GetCompanyCostsQuery { From = from, To = to }, cancellationToken);
            includesLabour &= costs.IncludesLabour;

            var project = Sum(projectByDay, from, to);
            var other = Sum(otherByDay, from, to);

            buckets.Add(new FinanceBucketDto
            {
                From = from,
                To = to,
                Expense = costs.Total,
                Revenue = project + other,
                RevenueProject = project,
                RevenueOther = other,
                Profit = project + other - costs.Total,
            });
        }

        var totals = Totals(request.From, request.To, buckets);

        var days = request.To.DayNumber - request.From.DayNumber + 1;
        var previousTo = request.From.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(days - 1));

        var previousCosts = await _sender.Send(
            new GetCompanyCostsQuery { From = previousFrom, To = previousTo },
            cancellationToken);

        var previousProject = await _context.ProjectRevenues.AsNoTracking()
            .Where(r => r.OccurredOn >= previousFrom && r.OccurredOn <= previousTo)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var previousOther = await _context.CompanyRevenues.AsNoTracking()
            .Where(r => r.OccurredOn >= previousFrom && r.OccurredOn <= previousTo)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        return new FinanceSeriesDto
        {
            Granularity = request.Granularity,
            IncludesLabour = includesLabour,
            Buckets = buckets,
            Totals = totals,
            Previous = Totals(
                previousFrom,
                previousTo,
                [
                    new FinanceBucketDto
                    {
                        From = previousFrom,
                        To = previousTo,
                        Expense = previousCosts.Total,
                        Revenue = previousProject + previousOther,
                        RevenueProject = previousProject,
                        RevenueOther = previousOther,
                        Profit = previousProject + previousOther - previousCosts.Total,
                    },
                ]),
        };
    }

    private sealed record DayAmount(DateOnly Day, decimal Amount);

    private static async Task<Dictionary<DateOnly, decimal>> SumByDayAsync(
        IQueryable<DayAmount> query,
        CancellationToken cancellationToken)
    {
        var rows = await query.ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Day, r => r.Amount);
    }

    private static decimal Sum(Dictionary<DateOnly, decimal> byDay, DateOnly from, DateOnly to)
    {
        var total = 0m;

        foreach (var (day, amount) in byDay)
        {
            if (day >= from && day <= to)
            {
                total += amount;
            }
        }

        return total;
    }

    private static FinanceTotalsDto Totals(DateOnly from, DateOnly to, IReadOnlyList<FinanceBucketDto> buckets)
    {
        var expense = buckets.Sum(b => b.Expense);
        var project = buckets.Sum(b => b.RevenueProject);
        var other = buckets.Sum(b => b.RevenueOther);
        var revenue = project + other;
        var profit = revenue - expense;

        return new FinanceTotalsDto
        {
            From = from,
            To = to,
            Expense = expense,
            Revenue = revenue,
            RevenueProject = project,
            RevenueOther = other,
            Profit = profit,
            MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100m, 1) : null,
        };
    }
}
