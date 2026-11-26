using Construction.Application.Features.Planning;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>The part of the board that changes by the minute: who has clocked in today, and how far from their site they are.</summary>
[Collection(DatabaseCollection.Name)]
public class PlanningAttendanceTests : IntegrationTestBase
{
    public PlanningAttendanceTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    // About 55 metres per 0.0005 degrees of latitude.
    private const double SiteLat = 45.8000;
    private const double SiteLon = 15.9800;

    private async Task<(Employee Worker, Employee Idle, Project Site)> SeedAsync()
    {
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var idle = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(async scope =>
        {
            await scope.Db.Projects.Where(p => p.Id == site.Id).ExecuteUpdateAsync(p => p
                .SetProperty(x => x.Latitude, SiteLat).SetProperty(x => x.Longitude, SiteLon)
                .SetProperty(x => x.ShiftStartTime, new TimeOnly(7, 0)));
            return 0;
        });

        foreach (var e in new[] { worker, idle })
        {
            await InScopeAs(UserRole.Admin, scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = e.Id, ProjectId = site.Id, From = Today.AddDays(-1), To = Today.AddDays(1) }));
        }

        return (worker, idle, site);
    }

    [Fact]
    public async Task The_board_gets_the_clock_in_time_and_the_distances_of_the_shift_and_of_the_last_position()
    {
        var (worker, idle, site) = await SeedAsync();

        await InScope(async scope =>
        {
            scope.Db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = worker.Id,
                ProjectId = site.Id,
                StartedAt = DateTime.UtcNow.AddMinutes(-20),
                StartLatitude = SiteLat + 0.0005,
                StartLongitude = SiteLon,
            });
            scope.Db.LocationRecords.Add(new LocationRecord
            {
                EmployeeId = worker.Id,
                Latitude = SiteLat + 0.003,
                Longitude = SiteLon,
                Accuracy = 12,
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                ReceivedAt = DateTime.UtcNow.AddMinutes(-5),
            });
            await scope.Db.SaveChangesAsync();
            return 0;
        });

        var result = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningAttendanceQuery()));

        var row = Assert.Single(result.Rows, r => r.EmployeeId == worker.Id);
        Assert.Equal(site.Id, row.SiteId);
        Assert.NotNull(row.ClockedInAt);
        Assert.Null(row.ClockedOutAt);
        Assert.InRange(row.ClockInMeters!.Value, 40, 70);
        Assert.InRange(row.LastPingMeters!.Value, 300, 360);
        Assert.InRange(row.LastPingMinutesAgo!.Value, 4, 6);
        Assert.Equal(12, row.LastPingAccuracy);

        // Posted but nothing yet: a row with the site and nothing else, which is what "not clocked in" is made of.
        var quiet = Assert.Single(result.Rows, r => r.EmployeeId == idle.Id);
        Assert.Equal(site.Id, quiet.SiteId);
        Assert.Null(quiet.ClockedInAt);
        Assert.Null(quiet.LastPingMinutesAgo);
        Assert.Matches(@"^\d\d:\d\d$", result.NowLocal);
        Assert.Equal(Today, result.Today);
    }

    [Fact]
    public async Task The_planning_view_carries_the_sites_coordinates_and_start_of_shift()
    {
        var (_, _, site) = await SeedAsync();

        var plan = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningQuery { From = Today, To = Today }));

        var p = Assert.Single(plan.Projects, x => x.Id == site.Id);
        Assert.Equal(SiteLat, p.Latitude);
        Assert.Equal(SiteLon, p.Longitude);
        Assert.Equal(new TimeOnly(7, 0), p.ShiftStartTime);
    }

    [Fact]
    public async Task A_worker_may_not_ask_for_the_boards_attendance()
    {
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ForbiddenAccessException>(
            () => InScopeAs(UserRole.Worker, scope => scope.Send(new GetPlanningAttendanceQuery())));
    }
}
