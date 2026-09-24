using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Dashboard.Commands;
using Construction.Application.Features.Dashboard.Models;
using Construction.Application.Features.Dashboard.Queries;
using Construction.Application.Features.Finance;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The breakdown of spending, one project's summary, and the settings a
/// dashboard widget keeps. Each test uses a year of its own so the shared
/// database's other rows cannot leak into the sums.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class FinanceBreakdownTests : IntegrationTestBase
{
    public FinanceBreakdownTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Distinct from the other finance tests' years, and before 2026 for the same reason.
    private static int nextYear = 1700;

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

    /// <summary>One project with labour, a manual entry, a project cost and a cost tied to none.</summary>
    private async Task<Guid> SeedProjectWithSpendingAsync(int year)
    {
        Guid projectId = default;

        await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);
            projectId = project.Id;

            var clocked = await TestData.SeedEmployeeAsync(scope);
            var start = new DateTime(year, 5, 6, 8, 0, 0, DateTimeKind.Utc);
            scope.Db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = clocked.Id,
                ProjectId = project.Id,
                StartedAt = start,
                EndedAt = start.AddHours(8).AddMinutes(20),
                Status = TimeEntryStatus.Approved,
            });
            // 8h20m at 10.01 an hour is 83.4166… — a fraction that has to be rounded once.
            scope.Db.EmployeeRates.Add(new EmployeeRate
            {
                EmployeeId = clocked.Id,
                RateType = RateType.Hourly,
                HourlyRate = 10.01m,
                StartDate = new DateOnly(year, 1, 1),
            });

            var paid = await TestData.SeedEmployeeAsync(scope);
            scope.Db.FinanceEntries.Add(new FinanceEntry
            {
                EmployeeId = paid.Id,
                ProjectId = project.Id,
                Kind = FinanceEntryKind.WorkerPaymentFixed,
                Amount = 300m,
                OccurredOn = new DateOnly(year, 5, 7),
            });

            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping, Amount = 50m, ProjectId = project.Id, OccurredOn = new DateOnly(year, 5, 8),
            });
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping, Amount = 20m, OccurredOn = new DateOnly(year, 5, 9),
            });

            await scope.Db.SaveChangesAsync();
        });

        return projectId;
    }

    [Fact]
    public async Task The_company_breakdown_adds_up_to_the_company_figure_the_overview_shows()
    {
        var year = FreshYear();
        await SeedProjectWithSpendingAsync(year);
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);

        var breakdown = await AsSuperAdminAsync(scope => scope.Send(new GetFinanceBreakdownQuery { From = from, To = to }));
        var series = await AsSuperAdminAsync(scope => scope.Send(new GetFinanceSeriesQuery
        {
            From = from, To = to, Granularity = FinanceGranularity.Month,
        }));

        Assert.Equal(breakdown.Items.Sum(i => i.Amount), breakdown.Total);
        Assert.Equal(series.Totals.Expense, breakdown.Total);
        Assert.Equal(83.42m, breakdown.Items.Single(i => i.Kind == "Labour").Amount);
        Assert.Equal(300m, breakdown.Items.Single(i => i.Kind == "ManualPay").Amount);
        Assert.Equal(70m, breakdown.Items.Single(i => i.Kind == "GeneralExpenses").Amount);
    }

    [Fact]
    public async Task A_projects_breakdown_adds_up_to_its_row_in_the_project_view_to_the_cent()
    {
        var year = FreshYear();
        var projectId = await SeedProjectWithSpendingAsync(year);
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);

        var breakdown = await AsSuperAdminAsync(scope => scope.Send(new GetFinanceBreakdownQuery
        {
            From = from, To = to, ProjectId = projectId,
        }));
        var byProject = await AsSuperAdminAsync(scope => scope.Send(new GetFinanceByProjectQuery { From = from, To = to }));
        var row = Assert.Single(byProject.Rows, r => r.ProjectId == projectId);

        Assert.Equal(projectId, breakdown.ProjectId);
        Assert.Equal(breakdown.Items.Sum(i => i.Amount), breakdown.Total);
        Assert.Equal(row.Expense, breakdown.Total);
        // 83.42 + 300 + 50: the cost tied to no project is not this project's.
        Assert.Equal(433.42m, breakdown.Total);
        // The fleet and the tools belong to no site.
        Assert.Equal(0m, breakdown.Items.Single(i => i.Kind == "Vehicles").Amount);
        Assert.Equal(0m, breakdown.Items.Single(i => i.Kind == "Tools").Amount);
    }

    [Fact]
    public async Task A_project_nothing_was_spent_on_has_a_breakdown_of_zeroes()
    {
        var year = FreshYear();
        Guid projectId = default;
        await InScope(async scope => projectId = (await TestData.SeedProjectAsync(scope)).Id);

        var breakdown = await AsSuperAdminAsync(scope => scope.Send(new GetFinanceBreakdownQuery
        {
            From = new DateOnly(year, 1, 1), To = new DateOnly(year, 12, 31), ProjectId = projectId,
        }));

        Assert.Equal(0m, breakdown.Total);
        Assert.All(breakdown.Items, item => Assert.Equal(0m, item.Amount));
    }

    [Theory]
    [InlineData(FinanceAccess.None)]
    [InlineData(FinanceAccess.StatisticsOnly)]
    public async Task Without_the_full_finance_right_the_breakdown_and_the_summary_are_refused(FinanceAccess finance)
    {
        var year = FreshYear();
        Guid projectId = default;
        await InScope(async scope => projectId = (await TestData.SeedProjectAsync(scope)).Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, finance, scope => scope.Send(new GetFinanceBreakdownQuery
            {
                From = new DateOnly(year, 1, 1), To = new DateOnly(year, 1, 31),
            })));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            AsAsync(UserRole.Admin, finance, scope => scope.Send(new GetProjectFinanceSummaryQuery
            {
                ProjectId = projectId, From = new DateOnly(year, 1, 1), To = new DateOnly(year, 1, 31),
            })));
    }

    [Fact]
    public async Task A_projects_summary_gives_the_period_and_everything_to_date_with_the_budget_measured_against_the_latter()
    {
        // Recent days, inside the window "to date" reaches back over for a project created now.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var inPeriod = today.AddDays(-40);
        var before = today.AddDays(-100);
        Guid projectId = default;

        await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);
            projectId = project.Id;
            project.ContractValue = 1000m;
            project.Budget = 400m;

            // 100 spent and 250 received in the period asked for; 150 and 250 before it.
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping, Amount = 100m, ProjectId = project.Id, OccurredOn = inPeriod,
            });
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping, Amount = 150m, ProjectId = project.Id, OccurredOn = before,
            });
            scope.Db.ProjectRevenues.Add(new ProjectRevenue { ProjectId = project.Id, Amount = 250m, OccurredOn = inPeriod });
            scope.Db.ProjectRevenues.Add(new ProjectRevenue { ProjectId = project.Id, Amount = 250m, OccurredOn = before });

            await scope.Db.SaveChangesAsync();
        });

        var summary = await AsSuperAdminAsync(scope => scope.Send(new GetProjectFinanceSummaryQuery
        {
            ProjectId = projectId, From = inPeriod.AddDays(-5), To = inPeriod.AddDays(5),
        }));

        Assert.Equal(250m, summary.Period.Revenue);
        Assert.Equal(100m, summary.Period.Expense);
        Assert.Equal(150m, summary.Period.Profit);

        Assert.Equal(500m, summary.ToDate.Revenue);
        Assert.Equal(250m, summary.ToDate.Expense);
        Assert.Equal(250m, summary.ToDate.Profit);

        Assert.Equal(1000m, summary.ContractValue);
        Assert.Equal(400m, summary.Budget);
        Assert.Equal(62.5m, summary.BudgetUsedPercent);
        Assert.Equal(50m, summary.ContractCollectedPercent);
    }

    [Fact]
    public async Task A_summary_of_a_project_that_does_not_exist_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new GetProjectFinanceSummaryQuery
            {
                ProjectId = Guid.NewGuid(), From = new DateOnly(2026, 1, 1), To = new DateOnly(2026, 1, 31),
            })));
    }

    [Fact]
    public async Task A_widgets_settings_are_saved_with_the_layout_and_come_back()
    {
        var saved = await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new SaveDashboardLayoutCommand
            {
                Widgets =
                [
                    new DashboardWidgetDto
                    {
                        Id = Guid.NewGuid(), Type = DashboardWidgetTypes.ProjectFocus, X = 0, Y = 0, W = 6, H = 12,
                        Settings = new Dictionary<string, string> { ["projectId"] = "abc" },
                    },
                    new DashboardWidgetDto { Id = Guid.NewGuid(), Type = DashboardWidgetTypes.CompanyKpi, X = 6, Y = 0, W = 6, H = 12 },
                ],
            });

            return await scope.Send(new GetDashboardLayoutQuery());
        });

        var focus = Assert.Single(saved.Widgets, w => w.Type == DashboardWidgetTypes.ProjectFocus);
        Assert.Equal("abc", focus.Settings!["projectId"]);
        Assert.Null(saved.Widgets.Single(w => w.Type == DashboardWidgetTypes.CompanyKpi).Settings);
    }

    [Fact]
    public async Task Too_many_or_too_long_settings_are_refused()
    {
        var tooMany = Enumerable.Range(0, 9).ToDictionary(i => $"k{i}", _ => "v");

        await Assert.ThrowsAsync<ValidationException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new SaveDashboardLayoutCommand
            {
                Widgets =
                [
                    new DashboardWidgetDto { Id = Guid.NewGuid(), Type = DashboardWidgetTypes.ProjectFocus, X = 0, Y = 0, W = 6, H = 12, Settings = tooMany },
                ],
            })));

        await Assert.ThrowsAsync<ValidationException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new SaveDashboardLayoutCommand
            {
                Widgets =
                [
                    new DashboardWidgetDto
                    {
                        Id = Guid.NewGuid(), Type = DashboardWidgetTypes.ProjectFocus, X = 0, Y = 0, W = 6, H = 12,
                        Settings = new Dictionary<string, string> { ["projectId"] = new string('x', 101) },
                    },
                ],
            })));
    }
}
