using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Finance;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Which running projects have spent what the office asked to be warned at, and
/// the statistics an account without the right to amounts may see.
/// </summary>
/// <remarks>
/// The alerts list every running project in the shared database, so each test
/// looks only at the projects it made itself.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class FinanceAlertsTests : IntegrationTestBase
{
    public FinanceAlertsTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Distinct from the other finance tests' years, and before 2026 for the same reason.
    private static int nextYear = 1600;

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

    private static DateOnly Recent => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-20);

    /// <summary>A running project that has cost <paramref name="spent"/>, with the limits given.</summary>
    private async Task<Guid> SeedAsync(
        decimal spent,
        decimal? budget = null,
        decimal? contract = null,
        BudgetAlertBasis? basis = null,
        int? warnPercent = null,
        ProjectStatus status = ProjectStatus.Active)
    {
        Guid id = default;

        await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);
            id = project.Id;
            project.Budget = budget;
            project.ContractValue = contract;
            project.BudgetAlertBasis = basis;
            project.BudgetWarnPercent = warnPercent;
            project.Status = status;

            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping, Amount = spent, ProjectId = project.Id, OccurredOn = Recent,
            });

            await scope.Db.SaveChangesAsync();
        });

        return id;
    }

    private async Task<BudgetAlertDto?> AlertFor(Guid projectId)
    {
        var alerts = await AsSuperAdminAsync(scope => scope.Send(new GetBudgetAlertsQuery()));
        return alerts.Alerts.SingleOrDefault(a => a.ProjectId == projectId);
    }

    [Fact]
    public async Task A_project_nearing_its_budget_warns_and_one_past_it_is_over()
    {
        var nearing = await SeedAsync(spent: 850m, budget: 1000m);
        var past = await SeedAsync(spent: 1200m, budget: 1000m);
        var comfortable = await SeedAsync(spent: 300m, budget: 1000m);

        var warning = await AlertFor(nearing);
        Assert.NotNull(warning);
        Assert.Equal("Warning", warning.Level);
        Assert.Equal(85m, warning.UsedPercent);
        Assert.Equal("Budget", warning.Basis);
        Assert.Equal(1000m, warning.Limit);
        Assert.Equal(850m, warning.Spent);

        var over = await AlertFor(past);
        Assert.NotNull(over);
        Assert.Equal("Over", over.Level);
        Assert.Equal(120m, over.UsedPercent);

        Assert.Null(await AlertFor(comfortable));
    }

    [Fact]
    public async Task Without_a_budget_spending_is_measured_against_the_contract_and_with_one_the_budget_wins()
    {
        var contractOnly = await SeedAsync(spent: 900m, contract: 1000m);
        // Both set, nothing chosen: the budget (500) is what 900 is measured against, not the contract (10000).
        var both = await SeedAsync(spent: 900m, budget: 500m, contract: 10000m);

        Assert.Equal("Contract", (await AlertFor(contractOnly))!.Basis);

        var bothAlert = await AlertFor(both);
        Assert.Equal("Budget", bothAlert!.Basis);
        Assert.Equal(180m, bothAlert.UsedPercent);
    }

    [Fact]
    public async Task A_project_can_choose_the_contract_even_when_it_has_a_budget()
    {
        var chosen = await SeedAsync(spent: 900m, budget: 500m, contract: 1000m, basis: BudgetAlertBasis.Contract);

        var alert = await AlertFor(chosen);

        Assert.Equal("Contract", alert!.Basis);
        Assert.Equal(90m, alert.UsedPercent);
    }

    [Fact]
    public async Task A_project_can_ask_to_be_warned_at_its_own_share()
    {
        var early = await SeedAsync(spent: 500m, budget: 1000m, warnPercent: 50);
        var late = await SeedAsync(spent: 850m, budget: 1000m, warnPercent: 90);

        Assert.Equal(50, (await AlertFor(early))!.WarnPercent);
        Assert.Null(await AlertFor(late));
    }

    [Fact]
    public async Task Finished_cancelled_and_unmeasurable_projects_are_left_out()
    {
        var completed = await SeedAsync(spent: 2000m, budget: 1000m, status: ProjectStatus.Completed);
        var cancelled = await SeedAsync(spent: 2000m, budget: 1000m, status: ProjectStatus.Cancelled);
        var nothingSet = await SeedAsync(spent: 2000m);
        var budgetChosenButNone = await SeedAsync(spent: 2000m, contract: 5000m, basis: BudgetAlertBasis.Budget);

        Assert.Null(await AlertFor(completed));
        Assert.Null(await AlertFor(cancelled));
        Assert.Null(await AlertFor(nothingSet));
        Assert.Null(await AlertFor(budgetChosenButNone));
    }

    [Fact]
    public async Task The_alerts_are_refused_without_the_full_finance_right()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope => scope.Send(new GetBudgetAlertsQuery())));
    }

    [Fact]
    public async Task How_to_be_warned_is_saved_and_refuses_what_could_never_warn()
    {
        Guid id = default;
        await InScope(async scope => id = (await TestData.SeedProjectAsync(scope)).Id);

        var saved = await AsSuperAdminAsync(scope => scope.Send(new SetProjectBudgetCommand
        {
            ProjectId = id, Budget = 1000m, AlertBasis = BudgetAlertBasis.Budget, WarnPercent = 70,
        }));
        Assert.Equal("Budget", saved.AlertBasis);
        Assert.Equal(70, saved.WarnPercent);

        // Measuring against a budget that is not there, or warning at 100%, could never work.
        await Assert.ThrowsAsync<ValidationException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new SetProjectBudgetCommand
            {
                ProjectId = id, Budget = null, AlertBasis = BudgetAlertBasis.Budget,
            })));
        await Assert.ThrowsAsync<ValidationException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new SetProjectBudgetCommand { ProjectId = id, Budget = 1000m, WarnPercent = 100 })));

        var cleared = await AsSuperAdminAsync(scope => scope.Send(new SetProjectBudgetCommand { ProjectId = id }));
        Assert.Null(cleared.Budget);
        Assert.Null(cleared.AlertBasis);
        Assert.Null(cleared.WarnPercent);
    }

    // ---- statistics ---------------------------------------------------

    /// <summary>March of a year of its own against the 31 days before it: 200 spent (100 before), 300 received (0 before).</summary>
    private async Task<(DateOnly From, DateOnly To)> SeedStatisticsAsync()
    {
        var year = FreshYear();

        await InScope(async scope =>
        {
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Bookkeeping, Amount = 150m, OccurredOn = new DateOnly(year, 3, 10) });
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Damage, Amount = 50m, OccurredOn = new DateOnly(year, 3, 11) });
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Bookkeeping, Amount = 100m, OccurredOn = new DateOnly(year, 2, 10) });
            scope.Db.CompanyRevenues.Add(new CompanyRevenue { Amount = 300m, OccurredOn = new DateOnly(year, 3, 20), Source = CompanyRevenueSource.Other });
            await scope.Db.SaveChangesAsync();
        });

        return (new DateOnly(year, 3, 1), new DateOnly(year, 3, 31));
    }

    [Fact]
    public async Task An_account_that_may_only_see_statistics_gets_percentages_and_no_amounts()
    {
        var (from, to) = await SeedStatisticsAsync();

        var stats = await AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope =>
            scope.Send(new GetFinanceStatisticsQuery { From = from, To = to }));

        // Spending doubled (100 -> 200); income has no earlier figure to compare with; profit went from -100 to 100.
        Assert.Equal(100m, stats.ExpenseChangePercent);
        Assert.Null(stats.RevenueChangePercent);
        Assert.Equal(200m, stats.ProfitChangePercent);

        // 150 + 50 are both "other costs": one share, all of it.
        var share = Assert.Single(stats.Shares);
        Assert.Equal("GeneralExpenses", share.Kind);
        Assert.Equal(100m, share.SharePercent);
    }

    [Fact]
    public async Task Statistics_are_refused_to_an_account_with_no_finance_right_at_all()
    {
        var (from, to) = await SeedStatisticsAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, FinanceAccess.None, scope => scope.Send(new GetFinanceStatisticsQuery { From = from, To = to })));
    }

    [Fact]
    public async Task Statistics_do_not_open_the_amounts()
    {
        var (from, to) = await SeedStatisticsAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope => scope.Send(new GetFinanceSeriesQuery { From = from, To = to })));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope => scope.Send(new GetFinanceByProjectQuery { From = from, To = to })));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, FinanceAccess.StatisticsOnly, scope => scope.Send(new GetFinanceBreakdownQuery { From = from, To = to })));
    }

    [Fact]
    public async Task A_period_with_no_spending_has_no_shares_and_no_change_to_report()
    {
        var year = FreshYear();

        var stats = await AsAsync(UserRole.Admin, FinanceAccess.Full, scope => scope.Send(new GetFinanceStatisticsQuery
        {
            From = new DateOnly(year, 3, 1), To = new DateOnly(year, 3, 31),
        }));

        Assert.Empty(stats.Shares);
        Assert.Null(stats.ExpenseChangePercent);
        Assert.Null(stats.RevenueChangePercent);
        Assert.Null(stats.ProfitChangePercent);
    }
}
