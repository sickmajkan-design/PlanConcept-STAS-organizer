using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Book;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Finance;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Overview;

public class OverviewMonthDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    /// <summary>Whether the month has a settlement; without one the figures come from the book and invoices.</summary>
    public bool HasSettlement { get; init; }

    /// <summary>Every settlement of the month is closed, so its figures are the ones kept at closing.</summary>
    public bool Locked { get; init; }

    public decimal Billing { get; init; }

    /// <summary>Income that is not billing for hours or sites: rentals and income entered in the book without a site.</summary>
    public decimal OtherIncome { get; init; }

    public decimal WorkersEarned { get; init; }

    public decimal SubcontractorsEarned { get; init; }

    public decimal OfficeEarned { get; init; }

    public decimal SiteCosts { get; init; }

    public decimal FixedCosts { get; init; }

    public decimal Hours { get; init; }

    public decimal Income => Billing + OtherIncome;

    public decimal Outflow => WorkersEarned + SubcontractorsEarned + OfficeEarned + SiteCosts + FixedCosts;

    public decimal Profit => Income - Outflow;
}

public class OverviewStepDto
{
    /// <summary>"Billing", "OtherIncome", "Workers", "Subcontractors", "Office", "SiteCosts", "FixedCosts" or "Profit".</summary>
    public string Key { get; init; } = null!;

    public decimal Amount { get; init; }

    /// <summary>Share of the period's income, in percent; null when there is no income.</summary>
    public decimal? SharePercent { get; init; }
}

public class OverviewDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>False for an account that sees statistics only: shares and margins are shown, amounts are zero.</summary>
    public bool AmountsVisible { get; init; }

    public decimal Income { get; init; }

    public decimal Outflow { get; init; }

    public decimal Profit { get; init; }

    public decimal Hours { get; init; }

    /// <summary>Profit per worked hour; null when there are no hours.</summary>
    public decimal? ProfitPerHour { get; init; }

    public decimal? MarginPercent { get; init; }

    /// <summary>Profit of the period of the same length that ends the day before this one starts.</summary>
    public decimal PreviousProfit { get; init; }

    public decimal PreviousIncome { get; init; }

    /// <summary>From hours to profit: each line with its share of income.</summary>
    public IReadOnlyList<OverviewStepDto> Steps { get; init; } = [];

    public IReadOnlyList<OverviewMonthDto> Months { get; init; } = [];

    public OverviewMonthDto? BestMonth { get; init; }

    public OverviewMonthDto? WorstMonth { get; init; }

    /// <summary>Months in the period whose settlement has not been closed.</summary>
    public int OpenMonths { get; init; }
}

internal static class OverviewCalculator
{
    public static async Task<List<OverviewMonthDto>> MonthsAsync(
        IApplicationDbContext context,
        DateOnly from,
        DateOnly to,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        var branchVehicles = await BranchScope.VehicleIdsAsync(context, branchId, cancellationToken);
        var branchTools = await BranchScope.ToolIdsAsync(context, branchId, cancellationToken);

        var months = new List<OverviewMonthDto>();

        for (var cursor = new DateOnly(from.Year, from.Month, 1);
             cursor <= to;
             cursor = cursor.AddMonths(1))
        {
            var first = cursor;
            var last = new DateOnly(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month));

            // ---- the settlement of the month -------------------------------

            var ledgers = context.Ledgers.AsNoTracking()
                .Where(l => l.Year == cursor.Year && l.Month == cursor.Month);

            if (branchId is { } id)
            {
                var token = BranchTree.Token(id);
                ledgers = ledgers.Where(l => l.Branch!.Path.Contains(token));
            }

            var ledgerRows = await ledgers
                .Select(l => new { l.Id, l.LockedAt, l.ResultJson })
                .ToListAsync(cancellationToken);

            var settlement = LedgerMonthResult.Empty;

            foreach (var ledger in ledgerRows)
            {
                var stored = ledger.LockedAt is not null ? LedgerMonthResult.Parse(ledger.ResultJson) : null;

                settlement += stored
                    ?? await LedgerMonthResults.ComputeAsync(context, ledger.Id, cancellationToken);
            }

            var hasSettlement = ledgerRows.Count > 0;

            // ---- what the book and the other tables hold ---------------------

            var entries = context.BookEntries.AsNoTracking()
                .InBranch(branchId)
                .Where(e => e.OccurredOn >= first && e.OccurredOn <= last);

            var fixedCosts = await entries
                .Where(e => e.Kind == BookEntryKind.Cost && e.Category.Bucket == BookCategoryBucket.Fixed)
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

            var siteCostEntries = await entries
                .Where(e => e.Kind == BookEntryKind.Cost && e.Category.Bucket == BookCategoryBucket.SiteCosts)
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

            var generalCosts = await context.GeneralExpenses.AsNoTracking()
                .InBranch(branchId)
                .Where(e => e.OccurredOn >= first && e.OccurredOn <= last
                    && e.Category != GeneralExpenseCategory.Housing)
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

            var toolCosts = await context.ToolExpenses.AsNoTracking()
                .Where(e => e.OccurredOn >= first && e.OccurredOn <= last)
                .Where(e => branchTools == null || branchTools.Contains(e.ToolId))
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

            var vehicleCosts = await context.VehicleExpenses.AsNoTracking()
                .Where(e => e.Status == VehicleExpenseStatus.Approved
                    && e.Kind != VehicleExpenseKind.Fuel
                    && e.OccurredOn >= first && e.OccurredOn <= last)
                .Where(e => branchVehicles == null || branchVehicles.Contains(e.VehicleId))
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

            var otherIncome = await context.CompanyRevenues.AsNoTracking()
                .InBranch(branchId)
                .Where(r => r.OccurredOn >= first && r.OccurredOn <= last)
                .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

            // Income entered in the book without a site is income of its own. One with a site is the billing
            // of that site, which the settlement already holds when there is one.
            otherIncome += await entries
                .Where(e => e.Kind == BookEntryKind.Income && e.ProjectId == null)
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

            var billing = settlement.Billing;
            var workers = settlement.WorkersEarned;
            var subcontractors = settlement.SubcontractorsEarned;
            var office = settlement.OfficeEarned;
            var siteCosts = settlement.SiteCosts + siteCostEntries + toolCosts + vehicleCosts;

            if (!hasSettlement)
            {
                // No settlement: what the month billed is what was invoiced for it, and what the workers
                // earned is what the book says was paid to them.
                var invoiced = context.Invoices.AsNoTracking()
                    .Where(i => i.Status != InvoiceStatus.Cancelled
                        && i.PayrollYear == cursor.Year && i.PayrollMonth == cursor.Month);

                if (branchId is { } branch)
                {
                    var token = BranchTree.Token(branch);
                    invoiced = invoiced.Where(i => i.Project.Branch!.Path.Contains(token));
                }

                billing = await invoiced.SumAsync(i => (decimal?)i.Amount, cancellationToken) ?? 0m;

                billing += await entries
                    .Where(e => e.Kind == BookEntryKind.Income && e.ProjectId != null)
                    .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

                var pay = await entries
                    .Where(e => e.Kind == BookEntryKind.Cost && e.Category.Bucket == BookCategoryBucket.WorkerEarnings)
                    .Select(e => new { e.Amount, e.Category.SystemKey })
                    .ToListAsync(cancellationToken);

                workers = pay.Where(p => p.SystemKey is not (BookRules.Keys.Subcontractors or BookRules.Keys.Office))
                    .Sum(p => p.Amount);
                subcontractors = pay.Where(p => p.SystemKey == BookRules.Keys.Subcontractors).Sum(p => p.Amount);
                office = pay.Where(p => p.SystemKey == BookRules.Keys.Office).Sum(p => p.Amount);
            }

            months.Add(new OverviewMonthDto
            {
                Year = cursor.Year,
                Month = cursor.Month,
                HasSettlement = hasSettlement,
                Locked = hasSettlement && ledgerRows.All(l => l.LockedAt is not null),
                Billing = billing,
                OtherIncome = otherIncome,
                WorkersEarned = workers,
                SubcontractorsEarned = subcontractors,
                OfficeEarned = office,
                SiteCosts = siteCosts,
                FixedCosts = fixedCosts + generalCosts,
                Hours = settlement.Hours,
            });
        }

        return months;
    }

    public static decimal Sum(IEnumerable<OverviewMonthDto> months, Func<OverviewMonthDto, decimal> pick) =>
        months.Sum(pick);

    public static decimal? Percent(decimal part, decimal whole) =>
        whole == 0m ? null : Math.Round(part / whole * 100m, 1, MidpointRounding.AwayFromZero);
}

/// <summary>
/// Money in, money out and profit for a period, from hours to profit. Profit follows the settlement (what was
/// earned and billed in the month); the cash flow below follows actual payments, and the two are not mixed.
/// </summary>
public record GetOverviewQuery : IRequest<OverviewDto>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetOverviewQueryValidator : AbstractValidator<GetOverviewQuery>
{
    public GetOverviewQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber <= 732)
            .WithMessage("A period cannot be longer than two years.")
            .OverridePropertyName(nameof(GetOverviewQuery.To));
    }
}

public class GetOverviewQueryHandler : IRequestHandler<GetOverviewQuery, OverviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOverviewQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<OverviewDto> Handle(GetOverviewQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureStatisticsAsync(_context, _currentUserService, cancellationToken);
        var amountsVisible = await FinanceRules.HasFullAsync(_context, _currentUserService, cancellationToken);

        var months = await OverviewCalculator.MonthsAsync(
            _context, request.From, request.To, request.BranchId, cancellationToken);

        var span = request.To.DayNumber - request.From.DayNumber + 1;
        var previousTo = request.From.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(span - 1));

        var previous = await OverviewCalculator.MonthsAsync(
            _context, previousFrom, previousTo, request.BranchId, cancellationToken);

        var income = months.Sum(m => m.Income);
        var outflow = months.Sum(m => m.Outflow);
        var profit = income - outflow;
        var hours = months.Sum(m => m.Hours);

        OverviewStepDto Step(string key, decimal amount) => new()
        {
            Key = key,
            Amount = amountsVisible ? amount : 0m,
            SharePercent = OverviewCalculator.Percent(amount, income),
        };

        var steps = new List<OverviewStepDto>
        {
            Step("Billing", months.Sum(m => m.Billing)),
            Step("OtherIncome", months.Sum(m => m.OtherIncome)),
            Step("Workers", months.Sum(m => m.WorkersEarned)),
            Step("Subcontractors", months.Sum(m => m.SubcontractorsEarned)),
            Step("Office", months.Sum(m => m.OfficeEarned)),
            Step("SiteCosts", months.Sum(m => m.SiteCosts)),
            Step("FixedCosts", months.Sum(m => m.FixedCosts)),
            Step("Profit", profit),
        };

        var withActivity = months.Where(m => m.Income != 0m || m.Outflow != 0m).ToList();

        OverviewMonthDto Hide(OverviewMonthDto m) => amountsVisible
            ? m
            : new OverviewMonthDto { Year = m.Year, Month = m.Month, HasSettlement = m.HasSettlement, Locked = m.Locked };

        return new OverviewDto
        {
            From = request.From,
            To = request.To,
            AmountsVisible = amountsVisible,
            Income = amountsVisible ? income : 0m,
            Outflow = amountsVisible ? outflow : 0m,
            Profit = amountsVisible ? profit : 0m,
            Hours = hours,
            ProfitPerHour = hours > 0 && amountsVisible ? Math.Round(profit / hours, 2) : null,
            MarginPercent = OverviewCalculator.Percent(profit, income),
            PreviousProfit = amountsVisible ? previous.Sum(m => m.Profit) : 0m,
            PreviousIncome = amountsVisible ? previous.Sum(m => m.Income) : 0m,
            Steps = steps,
            Months = months.Select(Hide).ToList(),
            BestMonth = withActivity.Count > 0 && amountsVisible
                ? withActivity.OrderByDescending(m => m.Profit).First()
                : null,
            WorstMonth = withActivity.Count > 0 && amountsVisible
                ? withActivity.OrderBy(m => m.Profit).First()
                : null,
            OpenMonths = months.Count(m => m.HasSettlement && !m.Locked),
        };
    }
}

public class CashflowDayDto
{
    public DateOnly Date { get; init; }

    public decimal Inflow { get; init; }

    public decimal Outflow { get; init; }

    public decimal Balance { get; init; }
}

public class CashflowDto
{
    public DateOnly From { get; init; }

    public int Days { get; init; }

    public decimal OpeningBalance { get; init; }

    public decimal TotalInflow { get; init; }

    public decimal TotalOutflow { get; init; }

    public decimal ClosingBalance { get; init; }

    /// <summary>The lowest the balance gets in the window, and the day it does.</summary>
    public decimal LowestBalance { get; init; }

    public DateOnly LowestDate { get; init; }

    public bool BelowZero { get; init; }

    public IReadOnlyList<CashflowDayDto> Series { get; init; } = [];
}

/// <summary>
/// The next days of cash: what is expected in (open invoices and incomes at their due date, or a few days
/// from today when it has passed) and what goes out (final payroll payments and costs at their due date).
/// </summary>
public record GetCashflowQuery : IRequest<CashflowDto>
{
    public int Days { get; init; } = 35;

    public decimal OpeningBalance { get; init; }

    /// <summary>How many days from today an overdue payment from a client is assumed to arrive.</summary>
    public int LateDays { get; init; } = 14;

    public Guid? BranchId { get; init; }
}

public class GetCashflowQueryValidator : AbstractValidator<GetCashflowQuery>
{
    public GetCashflowQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(7, 120);
        RuleFor(x => x.LateDays).InclusiveBetween(0, 120);
    }
}

public class GetCashflowQueryHandler : IRequestHandler<GetCashflowQuery, CashflowDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCashflowQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CashflowDto> Handle(GetCashflowQuery request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var end = today.AddDays(request.Days - 1);

        DateOnly Expected(DateOnly? due) =>
            due is { } d && d >= today ? d : today.AddDays(request.LateDays);

        DateOnly Planned(DateOnly? due) => due is { } d && d >= today ? d : today;

        var inflow = new Dictionary<DateOnly, decimal>();
        var outflow = new Dictionary<DateOnly, decimal>();

        void Add(Dictionary<DateOnly, decimal> into, DateOnly day, decimal amount)
        {
            if (day > end || amount <= 0m) return;
            into[day] = into.GetValueOrDefault(day) + amount;
        }

        var invoices = _context.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued && i.Amount > 0);

        if (request.BranchId is { } id)
        {
            var token = BranchTree.Token(id);
            invoices = invoices.Where(i => i.Project.Branch!.Path.Contains(token));
        }

        var open = await invoices
            .Select(i => new { i.DueDate, i.Amount, Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m })
            .ToListAsync(cancellationToken);

        foreach (var invoice in open)
        {
            Add(inflow, Expected(invoice.DueDate), invoice.Amount - invoice.Paid);
        }

        var unpaid = await _context.BookEntries.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.PaidOn == null && (e.Kind == BookEntryKind.Income || e.Kind == BookEntryKind.Cost))
            .Select(e => new { e.Kind, e.DueDate, e.Amount })
            .ToListAsync(cancellationToken);

        foreach (var entry in unpaid)
        {
            if (entry.Kind == BookEntryKind.Income)
            {
                Add(inflow, Expected(entry.DueDate), entry.Amount);
            }
            else
            {
                Add(outflow, Planned(entry.DueDate), entry.Amount);
            }
        }

        var finals = await _context.WagePayouts.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(p => p.Kind == PayoutKind.Final && p.PaidOn == null)
            .Select(p => new { p.PayDate, p.Amount })
            .ToListAsync(cancellationToken);

        foreach (var payout in finals)
        {
            Add(outflow, Planned(payout.PayDate), payout.Amount);
        }

        var series = new List<CashflowDayDto>();
        var balance = request.OpeningBalance;
        var lowest = balance;
        var lowestDate = today;

        for (var day = today; day <= end; day = day.AddDays(1))
        {
            var dayIn = inflow.GetValueOrDefault(day);
            var dayOut = outflow.GetValueOrDefault(day);
            balance += dayIn - dayOut;

            if (balance < lowest)
            {
                lowest = balance;
                lowestDate = day;
            }

            series.Add(new CashflowDayDto { Date = day, Inflow = dayIn, Outflow = dayOut, Balance = balance });
        }

        return new CashflowDto
        {
            From = today,
            Days = request.Days,
            OpeningBalance = request.OpeningBalance,
            TotalInflow = series.Sum(d => d.Inflow),
            TotalOutflow = series.Sum(d => d.Outflow),
            ClosingBalance = balance,
            LowestBalance = lowest,
            LowestDate = lowestDate,
            BelowZero = lowest < 0m,
            Series = series,
        };
    }
}

public class ForecastDto
{
    /// <summary>False when there are no closed months to base an estimate on.</summary>
    public bool HasData { get; init; }

    public int MonthsUsed { get; init; }

    public decimal Hours { get; init; }

    public decimal Income { get; init; }

    public decimal Outflow { get; init; }

    /// <summary>The estimate from the three months added together.</summary>
    public decimal Profit { get; init; }

    /// <summary>The worst and best of the three months, as a range, because it is an estimate.</summary>
    public decimal ProfitLow { get; init; }

    public decimal ProfitHigh { get; init; }
}

/// <summary>
/// An estimate for the next month from the last three closed ones: income and the costs that follow hours
/// are priced per hour of those months, while office and fixed costs are taken as they were last month.
/// </summary>
public record GetForecastQuery : IRequest<ForecastDto>
{
    /// <summary>Planned hours. Omitted means the average of the months used.</summary>
    public decimal? Hours { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetForecastQueryHandler : IRequestHandler<GetForecastQuery, ForecastDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetForecastQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ForecastDto> Handle(GetForecastQuery request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var from = thisMonth.AddMonths(-12);
        var to = thisMonth.AddDays(-1);

        var months = await OverviewCalculator.MonthsAsync(_context, from, to, request.BranchId, cancellationToken);

        var used = months
            .Where(m => m.HasSettlement && m.Locked && m.Hours > 0)
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
            .Take(3)
            .ToList();

        if (used.Count == 0)
        {
            return new ForecastDto();
        }

        var lastMonth = used[0];
        var averageHours = used.Average(m => m.Hours);
        var hours = request.Hours ?? averageHours;

        // Per hour, so that more or fewer planned hours move the estimate the way they should.
        decimal Estimate(decimal billing, decimal workers, decimal subcontractors, decimal siteCosts, decimal monthHours) =>
            monthHours == 0m
                ? 0m
                : hours * (billing - workers - subcontractors - siteCosts) / monthHours
                  - lastMonth.OfficeEarned - lastMonth.FixedCosts;

        var each = used
            .Select(m => Estimate(m.Billing, m.WorkersEarned, m.SubcontractorsEarned, m.SiteCosts, m.Hours))
            .ToList();

        var totalHours = used.Sum(m => m.Hours);
        var central = Estimate(
            used.Sum(m => m.Billing), used.Sum(m => m.WorkersEarned), used.Sum(m => m.SubcontractorsEarned),
            used.Sum(m => m.SiteCosts), totalHours);

        var income = totalHours == 0m ? 0m : hours * used.Sum(m => m.Billing) / totalHours;

        return new ForecastDto
        {
            HasData = true,
            MonthsUsed = used.Count,
            Hours = hours,
            Income = income,
            Outflow = income - central,
            Profit = central,
            ProfitLow = each.Min(),
            ProfitHigh = each.Max(),
        };
    }
}
