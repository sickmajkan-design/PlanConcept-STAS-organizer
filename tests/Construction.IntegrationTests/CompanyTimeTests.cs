using Construction.Application.Common;
using Construction.Application.Features.Costs.Commands.SetEmployeeRate;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Application.Features.TimeEntries.Commands.AutoCloseStaleShifts;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The company's own calendar. Every instant in the database is UTC, but a shift belongs to the day on the company's
/// clock: one that starts at 00:30 on a Saturday in Belgrade is a Saturday shift (priced as one) even though it is
/// still Friday in UTC. The rest of the suite runs with a UTC company clock, where the two agree; these run on the
/// real one and put it back.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CompanyTimeTests : IntegrationTestBase
{
    public CompanyTimeTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    /// <summary>Runs <paramref name="test"/> on Belgrade's clock and restores the suite's UTC one, whatever happens.</summary>
    private static async Task OnBelgradeClockAsync(Func<Task> test)
    {
        CompanyTime.Configure("Europe/Belgrade", out _);

        try
        {
            await test();
        }
        finally
        {
            CompanyTime.Configure("UTC", out _);
        }
    }

    private async Task<(Guid EmployeeId, Guid ProjectId, User Admin)> SeedPricedPersonAsync()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            scope.Db.Users.Where(u => u.Id == admin.Id).ExecuteUpdate(u => u.SetProperty(x => x.FinanceAccess, FinanceAccess.Full));
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

            return scope.Send(new SetEmployeeRateCommand
            {
                EmployeeId = employee.Id,
                HourlyRate = 100m,
                WeekendHourlyRate = 150m,
                StartDate = new DateOnly(2026, 1, 1),
            });
        });

        return (employee.Id, project.Id, admin);
    }

    private Task SeedShiftAsync(Guid employeeId, Guid projectId, DateTime startedAtUtc, int hours) =>
        InScope(async scope =>
        {
            scope.Db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = employeeId,
                ProjectId = projectId,
                StartedAt = DateTime.SpecifyKind(startedAtUtc, DateTimeKind.Utc),
                EndedAt = DateTime.SpecifyKind(startedAtUtc.AddHours(hours), DateTimeKind.Utc),
                Status = TimeEntryStatus.Approved,
                WorkType = WorkType.Regular,
            });

            await scope.Db.SaveChangesAsync();
        });

    private Task<GetProjectCostsQueryResult> LabourAsync(User admin, Guid projectId, DateOnly from, DateOnly to) =>
        InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);
            var report = await scope.Send(new GetProjectCostsQuery { From = from, To = to, ProjectId = projectId });

            return new GetProjectCostsQueryResult(report.Rows.Sum(r => r.LabourCost));
        });

    private sealed record GetProjectCostsQueryResult(decimal Labour);

    [Fact]
    public async Task A_shift_that_starts_after_midnight_on_a_saturday_is_a_saturday_shift_priced_at_the_weekend_rate()
    {
        await OnBelgradeClockAsync(async () =>
        {
            var (employee, project, admin) = await SeedPricedPersonAsync();

            // 00:30 on Saturday 7 March 2026 in Belgrade (UTC+1) is 23:30 on Friday in UTC.
            await SeedShiftAsync(employee, project, new DateTime(2026, 3, 6, 23, 30, 0), hours: 4);

            var saturday = new DateOnly(2026, 3, 7);
            var friday = new DateOnly(2026, 3, 6);

            // Four hours at the weekend rate, counted on the Saturday and not on the Friday.
            Assert.Equal(600m, (await LabourAsync(admin, project, saturday, saturday)).Labour);
            Assert.Equal(0m, (await LabourAsync(admin, project, friday, friday)).Labour);
        });
    }

    [Fact]
    public async Task A_shift_late_on_a_friday_evening_stays_on_the_friday_at_the_regular_rate()
    {
        await OnBelgradeClockAsync(async () =>
        {
            var (employee, project, admin) = await SeedPricedPersonAsync();

            // 23:30 on Friday 6 March 2026 in Belgrade is 22:30 UTC the same day.
            await SeedShiftAsync(employee, project, new DateTime(2026, 3, 6, 22, 30, 0), hours: 4);

            var friday = new DateOnly(2026, 3, 6);

            Assert.Equal(400m, (await LabourAsync(admin, project, friday, friday)).Labour);
        });
    }

    [Fact]
    public async Task The_day_boundary_follows_summer_time_too()
    {
        await OnBelgradeClockAsync(async () =>
        {
            var (employee, project, admin) = await SeedPricedPersonAsync();

            // 01:00 on Saturday 4 July 2026 in Belgrade (UTC+2) is 23:00 on Friday in UTC.
            await SeedShiftAsync(employee, project, new DateTime(2026, 7, 3, 23, 0, 0), hours: 2);

            var saturday = new DateOnly(2026, 7, 4);

            Assert.Equal(300m, (await LabourAsync(admin, project, saturday, saturday)).Labour);
        });
    }

    [Fact]
    public async Task A_shift_still_open_when_the_companys_day_ends_is_closed_at_that_midnight()
    {
        await OnBelgradeClockAsync(async () =>
        {
            var (employee, project, _) = await SeedPricedPersonAsync();
            // It is 00:30 on 19 October in Belgrade (summer time ended on the 25th, so UTC+2): 22:30 UTC on the 18th.
            // The shift began at 22:00 on the 18th, Belgrade time (20:00 UTC) and was never closed.
            await InScope(scope =>
            {
                scope.Clock.FreezeAt(new DateTime(2026, 10, 18, 22, 30, 0, DateTimeKind.Utc));
                return Task.CompletedTask;
            });

            await InScope(async scope =>
            {
                scope.Db.TimeEntries.Add(new TimeEntry
                {
                    EmployeeId = employee,
                    ProjectId = project,
                    StartedAt = new DateTime(2026, 10, 18, 20, 0, 0, DateTimeKind.Utc),
                    Status = TimeEntryStatus.InProgress,
                    WorkType = WorkType.Regular,
                });
                await scope.Db.SaveChangesAsync();
            });

            var closed = await InScope(scope => scope.Send(new AutoCloseStaleShiftsCommand()));

            // At least this one: other tests in the shared database may have left shifts open that are stale by this clock too.
            Assert.True(closed >= 1);

            var entry = await InScope(scope => scope.Db.TimeEntries.AsNoTracking().SingleAsync(t => t.EmployeeId == employee));
            // Closed at the company's midnight (22:00 UTC), not left open because the UTC date had not changed yet.
            Assert.Equal(new DateTime(2026, 10, 18, 22, 0, 0, DateTimeKind.Utc), entry.EndedAt);
            Assert.True(entry.AutoClosed);
        });
    }
}
