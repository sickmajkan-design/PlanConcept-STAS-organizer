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

    /// <summary>
    /// The month's costs by kind, for "where the money goes": "Workers", "Subcontractors", "Office", "Fuel",
    /// "Housing", "CarRental", "Contributions", "Bonus", "Tools", "Vehicles", "GeneralCosts", and the name of a
    /// category for what was entered in the book.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> Costs { get; init; } = new Dictionary<string, decimal>();

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

    /// <summary>Profit divided over the months of the period that had any activity.</summary>
    public decimal AverageMonthlyProfit { get; init; }

    /// <summary>Where the money goes: the period's costs by kind, biggest first, with each one's share of the outflow.</summary>
    public IReadOnlyList<OverviewCostDto> CostsByKind { get; init; } = [];
}

public class OverviewCostDto
{
    /// <summary>A kind ("Workers", "Fuel", …) or the name of a category entered in the book.</summary>
    public string Key { get; init; } = null!;

    public decimal Amount { get; init; }

    public decimal? SharePercent { get; init; }
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

            var costs = new Dictionary<string, decimal>();

            void AddCost(string key, decimal amount)
            {
                if (amount != 0m)
                {
                    costs[key] = costs.GetValueOrDefault(key) + amount;
                }
            }

            var fixedByCategory = await entries
                .Where(e => e.Kind == BookEntryKind.Cost && e.Category.Bucket == BookCategoryBucket.Fixed)
                .GroupBy(e => e.Category.Name)
                .Select(g => new { Name = g.Key, Amount = g.Sum(e => e.Amount) })
                .ToListAsync(cancellationToken);

            var siteByCategory = await entries
                .Where(e => e.Kind == BookEntryKind.Cost && e.Category.Bucket == BookCategoryBucket.SiteCosts)
                .GroupBy(e => e.Category.Name)
                .Select(g => new { Name = g.Key, Amount = g.Sum(e => e.Amount) })
                .ToListAsync(cancellationToken);

            var fixedCosts = fixedByCategory.Sum(c => c.Amount);
            var siteCostEntries = siteByCategory.Sum(c => c.Amount);

            foreach (var item in fixedByCategory) AddCost(item.Name, item.Amount);
            foreach (var item in siteByCategory) AddCost(item.Name, item.Amount);

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

            AddCost("Tools", toolCosts);
            AddCost("Vehicles", vehicleCosts);
            AddCost("GeneralCosts", generalCosts);

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

            AddCost("Workers", workers);
            AddCost("Subcontractors", subcontractors);
            AddCost("Office", office);
            AddCost("Fuel", settlement.Fuel);
            AddCost("Housing", settlement.Housing);
            AddCost("CarRental", settlement.Rent);
            AddCost("Contributions", settlement.Contributions);
            AddCost("Bonus", settlement.Bonus);

            months.Add(new OverviewMonthDto
            {
                Costs = costs,
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
            AverageMonthlyProfit = amountsVisible && withActivity.Count > 0
                ? Math.Round(withActivity.Sum(m => m.Profit) / withActivity.Count, 2)
                : 0m,
            CostsByKind = amountsVisible
                ? months
                    .SelectMany(m => m.Costs)
                    .GroupBy(c => c.Key)
                    .Select(g => new OverviewCostDto
                    {
                        Key = g.Key,
                        Amount = g.Sum(c => c.Value),
                        SharePercent = OverviewCalculator.Percent(g.Sum(c => c.Value), outflow),
                    })
                    .OrderByDescending(c => c.Amount)
                    .ToList()
                : [],
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

public class CashflowEventDto
{
    public DateOnly Date { get; init; }

    /// <summary>"Invoice", "Income", "Cost" or "Payroll".</summary>
    public string Kind { get; init; } = null!;

    /// <summary>The invoice number, or the description of the entry.</summary>
    public string? What { get; init; }

    /// <summary>The client or supplier, or the pay group.</summary>
    public string? Who { get; init; }

    /// <summary>Positive for money in, negative for money out.</summary>
    public decimal Amount { get; init; }

    public decimal BalanceAfter { get; init; }

    /// <summary>The day is an assumption: an overdue payment is expected a few days from today.</summary>
    public bool Estimated { get; init; }
}

public class CashflowDto
{
    public DateOnly From { get; init; }

    public int Days { get; init; }

    public decimal OpeningBalance { get; init; }

    public decimal TotalInflow { get; init; }

    public decimal TotalOutflow { get; init; }

    public decimal ClosingBalance { get; init; }

    /// <summary>What goes in and out, in date order, each with the balance after it.</summary>
    public IReadOnlyList<CashflowEventDto> Events { get; init; } = [];

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
        var events = new List<(DateOnly Date, string Kind, string? What, string? Who, decimal Amount, bool Estimated)>();

        void Add(
            Dictionary<DateOnly, decimal> into,
            DateOnly day,
            decimal amount,
            string kind,
            string? what,
            string? who,
            bool estimated = false)
        {
            if (day > end || amount <= 0m) return;
            into[day] = into.GetValueOrDefault(day) + amount;
            events.Add((day, kind, what, who, ReferenceEquals(into, inflow) ? amount : -amount, estimated));
        }

        var invoices = _context.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued && i.Amount > 0);

        if (request.BranchId is { } id)
        {
            var token = BranchTree.Token(id);
            invoices = invoices.Where(i => i.Project.Branch!.Path.Contains(token));
        }

        var open = await invoices
            .Select(i => new
            {
                i.DueDate,
                i.Amount,
                i.Number,
                Client = i.Project.Customer != null ? i.Project.Customer.Name : i.Project.Name,
                Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        foreach (var invoice in open)
        {
            var late = invoice.DueDate is not { } due || due < today;
            Add(inflow, Expected(invoice.DueDate), invoice.Amount - invoice.Paid, "Invoice", invoice.Number, invoice.Client, late);
        }

        var unpaid = await _context.BookEntries.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.PaidOn == null && (e.Kind == BookEntryKind.Income || e.Kind == BookEntryKind.Cost))
            .Select(e => new { e.Kind, e.DueDate, e.Amount, e.Description, e.Counterparty })
            .ToListAsync(cancellationToken);

        foreach (var entry in unpaid)
        {
            var late = entry.DueDate is not { } due || due < today;

            if (entry.Kind == BookEntryKind.Income)
            {
                Add(inflow, Expected(entry.DueDate), entry.Amount, "Income", entry.Description, entry.Counterparty, late);
            }
            else
            {
                Add(outflow, Planned(entry.DueDate), entry.Amount, "Cost", entry.Description, entry.Counterparty, late);
            }
        }

        var finals = await _context.WagePayouts.AsNoTracking()
            .InBranch(request.BranchId)
            .Where(p => p.Kind == PayoutKind.Final && p.PaidOn == null)
            .Select(p => new { p.PayDate, p.Amount, p.Group })
            .ToListAsync(cancellationToken);

        foreach (var payout in finals)
        {
            Add(outflow, Planned(payout.PayDate), payout.Amount, "Payroll", null, payout.Group.ToString(),
                payout.PayDate < today);
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

        var running = request.OpeningBalance;

        var ordered = events
            .OrderBy(e => e.Date)
            .ThenByDescending(e => e.Amount)
            .Select(e =>
            {
                running += e.Amount;

                return new CashflowEventDto
                {
                    Date = e.Date,
                    Kind = e.Kind,
                    What = e.What,
                    Who = e.Who,
                    Amount = e.Amount,
                    BalanceAfter = running,
                    Estimated = e.Estimated,
                };
            })
            .ToList();

        return new CashflowDto
        {
            Events = ordered,
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

    /// <summary>The average of the months used, which is what "hours" defaults to.</summary>
    public decimal AverageHours { get; init; }

    public decimal IncomePerHour { get; init; }

    /// <summary>What the costs that follow hours come to per hour.</summary>
    public decimal CostPerHour { get; init; }

    public decimal Workers { get; init; }

    public decimal Subcontractors { get; init; }

    public decimal SiteCosts { get; init; }

    public decimal Office { get; init; }

    public decimal Fixed { get; init; }

    /// <summary>The profit of the last closed month, for "against last month".</summary>
    public decimal LastMonthProfit { get; init; }

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

        var usedHours = used.Sum(m => m.Hours);

        decimal PerHour(decimal total) => usedHours == 0m ? 0m : hours * total / usedHours;

        return new ForecastDto
        {
            HasData = true,
            MonthsUsed = used.Count,
            Hours = hours,
            AverageHours = Math.Round(averageHours, 1),
            IncomePerHour = usedHours == 0m ? 0m : Math.Round(used.Sum(m => m.Billing) / usedHours, 2),
            CostPerHour = usedHours == 0m
                ? 0m
                : Math.Round(
                    (used.Sum(m => m.WorkersEarned) + used.Sum(m => m.SubcontractorsEarned) + used.Sum(m => m.SiteCosts))
                    / usedHours, 2),
            Workers = Math.Round(PerHour(used.Sum(m => m.WorkersEarned)), 2),
            Subcontractors = Math.Round(PerHour(used.Sum(m => m.SubcontractorsEarned)), 2),
            SiteCosts = Math.Round(PerHour(used.Sum(m => m.SiteCosts)), 2),
            Office = lastMonth.OfficeEarned,
            Fixed = lastMonth.FixedCosts,
            LastMonthProfit = lastMonth.Profit,
            Income = income,
            Outflow = income - central,
            Profit = central,
            ProfitLow = each.Min(),
            ProfitHigh = each.Max(),
        };
    }
}


public class OverviewAttentionDto
{
    /// <summary>"LateInvoice", "OpenMonth" or "Checks".</summary>
    public string Kind { get; init; } = null!;

    public int Count { get; init; }

    public decimal Amount { get; init; }

    /// <summary>For a late invoice, its number; for a month, "yyyy-MM".</summary>
    public string? Reference { get; init; }

    public int Days { get; init; }
}

/// <summary>
/// What needs a look on the money side: invoices past their due date, months whose settlement has gone by
/// without being closed, and the open items on the settlement's own check list.
/// </summary>
public record GetOverviewAttentionQuery : IRequest<IReadOnlyList<OverviewAttentionDto>>
{
    public Guid? BranchId { get; init; }
}

public class GetOverviewAttentionQueryHandler
    : IRequestHandler<GetOverviewAttentionQuery, IReadOnlyList<OverviewAttentionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISender _sender;

    public GetOverviewAttentionQueryHandler(
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

    public async Task<IReadOnlyList<OverviewAttentionDto>> Handle(
        GetOverviewAttentionQuery request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var items = new List<OverviewAttentionDto>();

        var late = _context.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Issued && i.Amount > 0 && i.DueDate != null && i.DueDate < today)
            .Where(i => i.Amount > (i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m));

        if (request.BranchId is { } id)
        {
            var token = BranchTree.Token(id);
            late = late.Where(i => i.Project.Branch!.Path.Contains(token));
        }

        var lateList = await late
            .OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.Number,
                i.DueDate,
                Open = i.Amount - (i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m),
            })
            .ToListAsync(cancellationToken);

        foreach (var invoice in lateList.Take(3))
        {
            items.Add(new OverviewAttentionDto
            {
                Kind = "LateInvoice",
                Count = 1,
                Amount = invoice.Open,
                Reference = invoice.Number,
                Days = today.DayNumber - invoice.DueDate!.Value.DayNumber,
            });
        }

        if (lateList.Count > 3)
        {
            items.Add(new OverviewAttentionDto
            {
                Kind = "LateInvoice",
                Count = lateList.Count - 3,
                Amount = lateList.Skip(3).Sum(i => i.Open),
            });
        }

        var thisMonth = new DateOnly(today.Year, today.Month, 1);

        var open = await _context.Ledgers.AsNoTracking()
            .Where(l => l.LockedAt == null
                && (l.Year < thisMonth.Year || (l.Year == thisMonth.Year && l.Month < thisMonth.Month)))
            .OrderBy(l => l.Year).ThenBy(l => l.Month)
            .Select(l => new { l.Id, l.Year, l.Month })
            .ToListAsync(cancellationToken);

        if (open.Count > 0)
        {
            items.Add(new OverviewAttentionDto
            {
                Kind = "OpenMonth",
                Count = open.Count,
                Reference = $"{open[0].Year}-{open[0].Month:00}",
            });
        }

        // The settlement's own list of what to look at, for the month that has just ended or is running.
        var checkMonths = await _context.Ledgers.AsNoTracking()
            .Where(l => l.LockedAt == null
                && l.Year * 12 + l.Month >= thisMonth.Year * 12 + thisMonth.Month - 1)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var checks = 0;

        foreach (var ledgerId in checkMonths)
        {
            checks += (await _sender.Send(
                new Ledgers.Queries.GetLedgerChecks.GetLedgerChecksQuery { LedgerId = ledgerId },
                cancellationToken)).Count;
        }

        if (checks > 0)
        {
            items.Add(new OverviewAttentionDto { Kind = "Checks", Count = checks });
        }

        return items;
    }
}
