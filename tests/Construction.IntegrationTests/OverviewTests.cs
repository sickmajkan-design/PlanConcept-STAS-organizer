using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Book;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.DeleteLedger;
using Construction.Application.Features.Ledgers.Commands.LockLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Overview;
using Construction.Application.Features.Payouts;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Closing a month, and the overview built from it. Each test uses a year of its own so the shared database's
/// other months cannot leak into the figures.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class OverviewTests : IntegrationTestBase
{
    public OverviewTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static int nextYear = 2011;

    private static int FreshYear() => Interlocked.Increment(ref nextYear);

    private async Task<T> AsAsync<T>(UserRole role, FinanceAccess finance, Func<TestScope, Task<T>> action)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, role);
            user.FinanceAccess = finance;
            await scope.Db.SaveChangesAsync();
            scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);

            return await action(scope);
        });
    }

    private Task<T> AsSuperAdminAsync<T>(Func<TestScope, Task<T>> action) =>
        AsAsync(UserRole.SuperAdmin, FinanceAccess.None, action);

    /// <summary>
    /// Every test works in a business unit of its own. The database outlives a run, so a year or a month is
    /// not private to a test; a unit is.
    /// </summary>
    private Task<Guid> SeedBranchAsync() =>
        AsSuperAdminAsync(async scope =>
            (await scope.Send(new CreateBranchCommand { Name = $"Jedinica {Guid.NewGuid():N}"[..20], Color = "#3457D5" })).Id);

    private sealed record Month(Guid LedgerId, Guid RowId, Guid HoursColumn, Guid RateColumn, int Year, int MonthNumber);

    /// <summary>
    /// A month with one worker: 160 hours at 20 earned (3200), billed at 33 an hour (5280).
    /// Profit before fixed costs is 2080.
    /// </summary>
    private Task<Month> SeedMonthAsync(int year, int month = 3, Guid? branchId = null) =>
        AsSuperAdminAsync(async scope =>
        {
            var ledger = await scope.Send(new CreateLedgerCommand
            {
                Name = $"Obracun {year}-{month}",
                Year = year,
                Month = month,
                BranchId = branchId,
                Template = LedgerTemplates.Payroll,
            });

            var section = await scope.Send(new AddLedgerSectionCommand { LedgerId = ledger.Id, Name = "Klijent" });
            var row = await scope.Send(new AddLedgerRowCommand { SectionId = section.Id, Label = "Radnik" });

            Guid Column(string key) => ledger.Columns.Single(c => c.SystemKey == key).Id;

            async Task Set(string key, string value) =>
                await scope.Send(new SetLedgerCellCommand { RowId = row.Id, ColumnId = Column(key), Value = value });

            await Set(LedgerTemplates.Keys.WorkerRate, "20");
            await Set(LedgerTemplates.Keys.Week(1), "80");
            await Set(LedgerTemplates.Keys.Week(2), "80");
            await Set(LedgerTemplates.Keys.ClientRate, "33");

            return new Month(
                ledger.Id, row.Id, Column(LedgerTemplates.Keys.Week(1)), Column(LedgerTemplates.Keys.ClientRate),
                year, month);
        });

    private Task<OverviewDto> OverviewAsync(int year, Guid branchId, int fromMonth = 1, int toMonth = 12) =>
        AsSuperAdminAsync(scope => scope.Send(new GetOverviewQuery
        {
            From = new DateOnly(year, fromMonth, 1),
            To = new DateOnly(year, toMonth, DateTime.DaysInMonth(year, toMonth)),
            BranchId = branchId,
        }));

    [Fact]
    public async Task Profit_is_billing_less_what_workers_earned_and_the_fixed_costs_of_the_month()
    {
        var year = FreshYear();
        var branch = await SeedBranchAsync();
        await SeedMonthAsync(year, branchId: branch);

        await AsSuperAdminAsync(async scope =>
        {
            var fixedCosts = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts).Select(c => c.Id).SingleAsync();

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost, Amount = 100m, OccurredOn = new DateOnly(year, 3, 10),
                CategoryId = fixedCosts, Description = "Knjigovodstvo", BranchId = branch,
            });

            return 0;
        });

        var overview = await OverviewAsync(year, branch);
        var march = overview.Months.Single(m => m.Month == 3);

        Assert.True(march.HasSettlement);
        Assert.Equal(5280m, march.Billing);
        Assert.Equal(3200m, march.WorkersEarned);
        Assert.Equal(100m, march.FixedCosts);
        Assert.Equal(160m, march.Hours);
        Assert.Equal(1980m, march.Profit);

        Assert.Equal(5280m, overview.Income);
        Assert.Equal(3300m, overview.Outflow);
        Assert.Equal(1980m, overview.Profit);
        Assert.Equal(12.38m, overview.ProfitPerHour);
        Assert.Equal(37.5m, overview.MarginPercent);

        Assert.Equal(1980m, overview.Steps.Single(s => s.Key == "Profit").Amount);
        Assert.Equal(60.6m, overview.Steps.Single(s => s.Key == "Workers").SharePercent);
        Assert.Equal(3, overview.BestMonth!.Month);
    }

    [Fact]
    public async Task A_closed_month_keeps_its_figures_when_the_sources_change()
    {
        var year = FreshYear();
        var branch = await SeedBranchAsync();
        var month = await SeedMonthAsync(year, branchId: branch);

        await AsSuperAdminAsync(scope => scope.Send(new LockLedgerCommand(month.LedgerId)));

        var locked = await OverviewAsync(year, branch);
        Assert.True(locked.Months.Single(m => m.Month == 3).Locked);
        Assert.Equal(2080m, locked.Profit);

        // A change made behind the lock, straight in the database, as a price change at the source would be.
        await InScope(scope => scope.Db.LedgerCells
            .Where(c => c.RowId == month.RowId && c.ColumnId == month.RateColumn)
            .ExecuteUpdateAsync(c => c.SetProperty(x => x.Value, "50")));

        var after = await OverviewAsync(year, branch);

        Assert.Equal(2080m, after.Profit);
        Assert.Equal(5280m, after.Income);
    }

    [Fact]
    public async Task A_closed_month_refuses_changes_until_it_is_reopened_with_a_reason()
    {
        var year = FreshYear();
        var month = await SeedMonthAsync(year);

        var locked = await AsSuperAdminAsync(scope => scope.Send(new LockLedgerCommand(month.LedgerId)));
        Assert.NotNull(locked.LockedAt);
        Assert.Equal(5280m, locked.Result!.Billing);

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new SetLedgerCellCommand
            {
                RowId = month.RowId, ColumnId = month.HoursColumn, Value = "10",
            });

            return 0;
        }));

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(scope =>
            scope.Send(new AddLedgerSectionCommand { LedgerId = month.LedgerId, Name = "Nova sekcija" })));

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new DeleteLedgerCommand(month.LedgerId));
            return 0;
        }));

        await Assert.ThrowsAnyAsync<Exception>(() => AsSuperAdminAsync(scope =>
            scope.Send(new UnlockLedgerCommand { Id = month.LedgerId, Reason = "" })));

        var reopened = await AsSuperAdminAsync(scope => scope.Send(new UnlockLedgerCommand
        {
            Id = month.LedgerId, Reason = "Ispravak sati",
        }));

        Assert.Null(reopened.LockedAt);
        Assert.Equal("Ispravak sati", reopened.UnlockNote);

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new SetLedgerCellCommand
            {
                RowId = month.RowId, ColumnId = month.HoursColumn, Value = "90",
            });

            return 0;
        });
    }

    [Fact]
    public async Task A_month_cannot_be_closed_twice_or_reopened_when_it_is_open()
    {
        var year = FreshYear();
        var month = await SeedMonthAsync(year);

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(scope =>
            scope.Send(new UnlockLedgerCommand { Id = month.LedgerId, Reason = "x" })));

        await AsSuperAdminAsync(scope => scope.Send(new LockLedgerCommand(month.LedgerId)));

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(scope =>
            scope.Send(new LockLedgerCommand(month.LedgerId))));
    }

    [Fact]
    public async Task The_cash_flow_goes_below_zero_when_wages_leave_before_the_client_pays()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projectId = await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);

            scope.Db.Invoices.Add(new Invoice
            {
                ProjectId = project.Id,
                Number = $"R-{Guid.NewGuid():N}"[..14],
                IssueDate = today.AddDays(-20),
                DueDate = today.AddDays(20),
                Amount = 5000m,
                PayrollYear = today.Year,
                PayrollMonth = today.Month,
            });

            await scope.Db.SaveChangesAsync();

            return project.Id;
        });

        await AsSuperAdminAsync(scope => scope.Send(new CreateWagePayoutCommand
        {
            Year = today.Year,
            Month = today.Month,
            Group = PayoutGroup.Workers,
            Kind = PayoutKind.Final,
            PayDate = today.AddDays(5),
            Amount = 1500m,
        }));

        var cashflow = await AsSuperAdminAsync(scope => scope.Send(new GetCashflowQuery
        {
            Days = 35, OpeningBalance = 200m, BranchId = null,
        }));

        Assert.Equal(35, cashflow.Series.Count);
        Assert.True(cashflow.BelowZero);
        Assert.True(cashflow.LowestBalance <= 200m - 1500m);
        Assert.True(cashflow.LowestDate < today.AddDays(20));
        Assert.True(cashflow.TotalInflow >= 5000m);
        Assert.NotEqual(Guid.Empty, projectId);
    }

    [Fact]
    public async Task The_forecast_is_a_range_from_the_last_closed_months()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var back = 1; back <= 3; back++)
        {
            var date = new DateOnly(today.Year, today.Month, 1).AddMonths(-back);
            var month = await SeedMonthAsync(date.Year, date.Month);
            await AsSuperAdminAsync(scope => scope.Send(new LockLedgerCommand(month.LedgerId)));
        }

        var forecast = await AsSuperAdminAsync(scope => scope.Send(new GetForecastQuery()));

        Assert.True(forecast.HasData);
        Assert.InRange(forecast.MonthsUsed, 1, 3);
        Assert.True(forecast.ProfitLow <= forecast.Profit + 0.01m);
        Assert.True(forecast.Profit <= forecast.ProfitHigh + 0.01m);
    }

    [Fact]
    public async Task An_account_with_statistics_only_sees_shares_but_no_amounts()
    {
        var year = FreshYear();
        var branch = await SeedBranchAsync();
        await SeedMonthAsync(year, branchId: branch);

        var overview = await AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope =>
            scope.Send(new GetOverviewQuery
            {
                From = new DateOnly(year, 1, 1), To = new DateOnly(year, 12, 31), BranchId = branch,
            }));

        Assert.False(overview.AmountsVisible);
        Assert.Equal(0m, overview.Profit);
        Assert.Equal(0m, overview.Income);
        Assert.Equal(0m, overview.Months.Sum(m => m.Billing));
        Assert.NotNull(overview.MarginPercent);
    }

    [Fact]
    public async Task Without_a_settlement_the_month_is_read_from_invoices_and_the_book()
    {
        var year = FreshYear();
        var branch = await SeedBranchAsync();

        await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);
            project.BranchId = branch;
            await scope.Db.SaveChangesAsync();

            scope.Db.Invoices.Add(new Invoice
            {
                ProjectId = project.Id,
                Number = $"R-{Guid.NewGuid():N}"[..14],
                IssueDate = new DateOnly(year, 5, 2),
                Amount = 2000m,
                PayrollYear = year,
                PayrollMonth = 5,
            });

            await scope.Db.SaveChangesAsync();
        });

        var overview = await OverviewAsync(year, branch, 5, 5);
        var may = overview.Months.Single();

        Assert.False(may.HasSettlement);
        Assert.Equal(2000m, may.Billing);
        Assert.Equal(2000m, overview.Profit);
        Assert.Equal(0, overview.OpenMonths);
    }
}
