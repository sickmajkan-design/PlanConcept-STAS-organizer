using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Finance;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Income, spending and profit over a period. Each test uses a year of its own
/// so the shared database's other rows cannot leak into the sums.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class FinanceSeriesTests : IntegrationTestBase
{
    public FinanceSeriesTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Distinct from CompanyCostsTests' range, and before 2026 for the same reason.
    private static int nextYear = 1900;

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

    [Fact]
    public async Task Revenue_and_spending_are_summed_per_bar_and_the_bars_add_up_to_the_totals()
    {
        var year = FreshYear();

        await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);

            scope.Db.ProjectRevenues.Add(new ProjectRevenue { ProjectId = project.Id, Amount = 1000m, OccurredOn = new DateOnly(year, 3, 5) });
            scope.Db.CompanyRevenues.Add(new CompanyRevenue { Amount = 250m, OccurredOn = new DateOnly(year, 3, 20), Source = CompanyRevenueSource.Other });
            scope.Db.CompanyRevenues.Add(new CompanyRevenue { Amount = 100m, OccurredOn = new DateOnly(year, 4, 2), Source = CompanyRevenueSource.Other });
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Bookkeeping, Amount = 40m, OccurredOn = new DateOnly(year, 3, 10) });

            await scope.Db.SaveChangesAsync();
        });

        var series = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new GetFinanceSeriesQuery
        {
            From = new DateOnly(year, 3, 1),
            To = new DateOnly(year, 4, 30),
            Granularity = FinanceGranularity.Month,
        }));

        Assert.Equal(2, series.Buckets.Count);
        Assert.Equal(1250m, series.Buckets[0].Revenue);
        Assert.Equal(1000m, series.Buckets[0].RevenueProject);
        Assert.Equal(250m, series.Buckets[0].RevenueOther);
        Assert.Equal(40m, series.Buckets[0].Expense);
        Assert.Equal(1210m, series.Buckets[0].Profit);
        Assert.Equal(100m, series.Buckets[1].Revenue);

        Assert.Equal(series.Buckets.Sum(b => b.Revenue), series.Totals.Revenue);
        Assert.Equal(series.Buckets.Sum(b => b.Expense), series.Totals.Expense);
        Assert.Equal(1350m, series.Totals.Revenue);
        Assert.Equal(40m, series.Totals.Expense);
        Assert.Equal(1310m, series.Totals.Profit);
    }

    [Fact]
    public async Task The_previous_period_is_the_same_length_ending_the_day_before()
    {
        var year = FreshYear();

        await InScope(async scope =>
        {
            scope.Db.CompanyRevenues.Add(new CompanyRevenue { Amount = 500m, OccurredOn = new DateOnly(year, 2, 10), Source = CompanyRevenueSource.Other });
            await scope.Db.SaveChangesAsync();
        });

        var series = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new GetFinanceSeriesQuery
        {
            From = new DateOnly(year, 3, 1),
            To = new DateOnly(year, 3, 31),
            Granularity = FinanceGranularity.Week,
        }));

        Assert.Equal(new DateOnly(year, 2, 28 + (DateTime.IsLeapYear(year) ? 1 : 0)), series.Previous.To);
        Assert.Equal(31, series.Previous.To.DayNumber - series.Previous.From.DayNumber + 1);
        Assert.Equal(500m, series.Previous.Revenue);
        Assert.Equal(0m, series.Totals.Revenue);
        Assert.Null(series.Totals.MarginPercent);
    }

    [Theory]
    [InlineData(FinanceAccess.None)]
    [InlineData(FinanceAccess.StatisticsOnly)]
    public async Task Without_the_full_finance_right_the_series_is_refused(FinanceAccess finance)
    {
        var year = FreshYear();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, finance, scope => scope.Send(new GetFinanceSeriesQuery
            {
                From = new DateOnly(year, 1, 1),
                To = new DateOnly(year, 1, 31),
            })));
    }

    [Fact]
    public async Task Recording_a_company_revenue_needs_the_full_finance_right()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope => scope.Send(new RecordCompanyRevenueCommand
            {
                Amount = 10m,
                Source = CompanyRevenueSource.Other,
            })));

        var recorded = await AsAsync(UserRole.Admin, FinanceAccess.Full, scope => scope.Send(new RecordCompanyRevenueCommand
        {
            Amount = 10m,
            Source = CompanyRevenueSource.Other,
        }));

        Assert.Equal(10m, recorded.Amount);
    }

    [Fact]
    public async Task A_vehicle_can_only_be_named_on_a_vehicle_rental()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
            {
                var vehicle = await TestData.SeedVehicleAsync(scope);

                return await scope.Send(new RecordCompanyRevenueCommand
                {
                    Amount = 10m,
                    Source = CompanyRevenueSource.ToolRental,
                    VehicleId = vehicle.Id,
                });
            }));
    }

    [Fact]
    public async Task By_project_adds_a_subcontractors_flat_pay_to_that_projects_spending_and_puts_revenue_against_it()
    {
        var year = FreshYear();
        var day = new DateOnly(year, 6, 10);
        Guid projectId = default;

        await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);
            projectId = project.Id;

            var subcontractor = await TestData.SeedEmployeeAsync(scope);
            subcontractor.Type = EmployeeType.Subcontractor;

            scope.Db.FinanceEntries.Add(new FinanceEntry
            {
                EmployeeId = subcontractor.Id,
                ProjectId = project.Id,
                Kind = FinanceEntryKind.WorkerPaymentFixed,
                Amount = 700m,
                OccurredOn = day,
            });
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping,
                Amount = 50m,
                ProjectId = project.Id,
                OccurredOn = day,
            });
            scope.Db.ProjectRevenues.Add(new ProjectRevenue { ProjectId = project.Id, Amount = 2000m, OccurredOn = day });

            await scope.Db.SaveChangesAsync();
        });

        var result = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new GetFinanceByProjectQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
        }));

        var row = Assert.Single(result.Rows, r => r.ProjectId == projectId);
        Assert.Equal(700m, row.SubcontractorPay);
        Assert.Equal(750m, row.Expense);
        Assert.Equal(2000m, row.Revenue);
        Assert.Equal(1250m, row.Profit);
        Assert.Equal(62.5m, row.MarginPercent);
    }

    [Fact]
    public async Task A_projects_budget_can_be_set_and_cleared_only_with_the_full_finance_right()
    {
        Guid projectId = default;

        await InScope(async scope => projectId = (await TestData.SeedProjectAsync(scope)).Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope =>
                scope.Send(new SetProjectBudgetCommand { ProjectId = projectId, Budget = 1000m })));

        var set = await AsAsync(UserRole.Admin, FinanceAccess.Full, scope =>
            scope.Send(new SetProjectBudgetCommand { ProjectId = projectId, Budget = 1000m }));
        Assert.Equal(1000m, set.Budget);

        var cleared = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope =>
            scope.Send(new SetProjectBudgetCommand { ProjectId = projectId, Budget = null }));
        Assert.Null(cleared.Budget);
    }
}
