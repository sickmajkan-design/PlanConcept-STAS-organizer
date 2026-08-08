using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;
using Construction.Application.Features.Employees.Commands.DeleteEmployee;
using Construction.Application.Features.Locations.Commands.ReportLocations;
using Construction.Application.Features.Locations.Queries.GetCurrentLocations;
using Construction.Application.Features.Locations.Queries.GetLastLocation;
using Construction.Application.Features.Locations.Queries.GetLocationHistory;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// GPS ingest and the three queries that read it back.
/// </summary>
/// <remarks>
/// <para>
/// The ingest handler had no test at all — only its validator did, which
/// checks the shape of a batch and nothing about where the pings end up.
/// The property that matters most is not shape: the employee identity comes
/// from the token and is never taken from the payload, so a device cannot
/// report a position for somebody else. Nothing in the type system prevents
/// that from being changed.
/// </para>
/// <para>
/// The rest is provider-specific in ways an in-memory suite would report
/// green on: a <c>timestamptz</c> column rejects a non-UTC value outright,
/// and the map query is a lateral join picking the newest row per employee.
/// </para>
/// <para>
/// <c>GetCurrentLocations</c> reads across the whole table, so every assertion
/// here names the employees it seeded rather than counting rows — other tests'
/// data is in there too.
/// </para>
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class LocationTests : IntegrationTestBase
{
    public LocationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateTime Noon = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private static LocationPing Ping(DateTime at, double lat = 45.81, double lon = 15.98) =>
        new() { Latitude = lat, Longitude = lon, Timestamp = at };

    /// <summary>Seeds an employee with a signed-in account linked to them.</summary>
    private async Task<(Employee Employee, User User)> SeedWorkerAsync(
        EmployeeStatus status = EmployeeStatus.Active)
    {
        return await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope, status: status);
            var user = await TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id);
            return (employee, user);
        });
    }

    /// <summary>
    /// The markers on the map, with the page envelope unwrapped.
    /// </summary>
    /// <remarks>
    /// The query is paged now, but every test below is about which employees
    /// appear rather than about paging, and a page of 1000 is more than this
    /// suite ever seeds. The paging itself is asserted separately at the end.
    /// </remarks>
    private async Task<IReadOnlyCollection<Construction.Application.Features.Locations.Models.EmployeeLocationDto>> MapAsync(
        GetCurrentLocationsQuery query)
    {
        var page = await InScope(scope => scope.Send(query with
        {
            PageSize = GetCurrentLocationsQuery.MaxPageSize
        }));

        return page.Items;
    }

    private static void ActAs(TestScope scope, User user, Guid? employeeId) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    private async Task ReportAsync(User user, Guid employeeId, params LocationPing[] pings)
    {
        await InScope(scope =>
        {
            ActAs(scope, user, employeeId);
            return scope.Send(new ReportLocationsCommand { Pings = pings });
        });
    }

    // ---- ingest ----------------------------------------------------------

    [Fact]
    public async Task A_reported_ping_is_stored_against_the_employee_in_the_token()
    {
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(user, employee.Id, Ping(Noon));

        var stored = await InScope(scope => scope.Db.LocationRecords
            .Where(l => l.EmployeeId == employee.Id)
            .SingleAsync());

        Assert.Equal(45.81, stored.Latitude, 5);
        Assert.Equal(15.98, stored.Longitude, 5);
        Assert.Equal(Noon, stored.Timestamp);
    }

    [Fact]
    public async Task A_device_cannot_report_a_position_for_another_employee()
    {
        // The command has no employee field, and this is the test that says
        // adding one would be a bug rather than a feature. Without it, "the
        // identity comes from the token" is a comment.
        var (mine, user) = await SeedWorkerAsync();
        var someoneElse = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await ReportAsync(user, mine.Id, Ping(Noon));

        var theirs = await InScope(scope => scope.Db.LocationRecords
            .CountAsync(l => l.EmployeeId == someoneElse.Id));

        Assert.Equal(0, theirs);
    }

    [Fact]
    public async Task An_account_with_no_employee_behind_it_cannot_report_at_all()
    {
        // An office admin's account has no employee record. Letting the ping
        // through would need somewhere to put it, and there is nowhere.
        var user = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, user, employeeId: null);
            return scope.Send(new ReportLocationsCommand { Pings = [Ping(Noon)] });
        }));
    }

    [Fact]
    public async Task A_whole_offline_batch_is_stored_in_one_go()
    {
        // The point of batching: a phone out of coverage buffers and flushes.
        var (employee, user) = await SeedWorkerAsync();

        var batch = Enumerable.Range(0, 40)
            .Select(minute => Ping(Noon.AddMinutes(minute)))
            .ToArray();

        await ReportAsync(user, employee.Id, batch);

        var count = await InScope(scope => scope.Db.LocationRecords
            .CountAsync(l => l.EmployeeId == employee.Id));

        Assert.Equal(40, count);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public async Task A_timestamp_without_a_UTC_kind_is_normalised_before_it_reaches_the_column(
        DateTimeKind kind)
    {
        // Npgsql throws on a non-UTC DateTime bound to timestamptz, so getting
        // this wrong is not a subtly wrong row — it is the whole batch
        // rejected, on the ingest path a phone retries forever.
        var (employee, user) = await SeedWorkerAsync();

        var local = DateTime.SpecifyKind(new DateTime(2026, 8, 5, 12, 0, 0), kind);

        await ReportAsync(user, employee.Id, Ping(local));

        var stored = await InScope(scope => scope.Db.LocationRecords
            .Where(l => l.EmployeeId == employee.Id)
            .SingleAsync());

        Assert.Equal(DateTimeKind.Utc, stored.Timestamp.Kind);

        var expected = kind == DateTimeKind.Local ? local.ToUniversalTime() : Noon;
        Assert.Equal(expected, stored.Timestamp);
    }

    [Fact]
    public async Task The_server_records_when_it_received_the_ping_not_only_when_it_was_taken()
    {
        // A ping buffered offline for six hours arrives with an old capture
        // time. Without ReceivedAt there is no way to tell that apart from a
        // device whose clock is wrong.
        var (employee, user) = await SeedWorkerAsync();

        await InScope(scope =>
        {
            scope.Clock.FreezeAt(Noon.AddHours(6));
            ActAs(scope, user, employee.Id);
            return scope.Send(new ReportLocationsCommand { Pings = [Ping(Noon)] });
        });

        var stored = await InScope(scope => scope.Db.LocationRecords
            .Where(l => l.EmployeeId == employee.Id)
            .SingleAsync());

        Assert.Equal(Noon, stored.Timestamp);
        Assert.Equal(Noon.AddHours(6), stored.ReceivedAt);
    }

    // ---- the live map ----------------------------------------------------

    [Fact]
    public async Task The_map_shows_the_newest_ping_and_not_the_last_one_written()
    {
        // Written deliberately out of order. Ordering by insertion would pass
        // a test that reported them in order and be wrong the moment a batch
        // arrives late, which is the normal case for a phone coming back into
        // coverage.
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(
            user,
            employee.Id,
            Ping(Noon.AddMinutes(10), lat: 45.10),
            Ping(Noon.AddMinutes(30), lat: 45.30),
            Ping(Noon.AddMinutes(20), lat: 45.20));

        var map = await MapAsync(new GetCurrentLocationsQuery());

        var mine = Assert.Single(map, l => l.EmployeeId == employee.Id);

        Assert.Equal(45.30, mine.Latitude, 5);
        Assert.Equal(Noon.AddMinutes(30), mine.Timestamp);
    }

    [Fact]
    public async Task An_employee_who_has_never_reported_is_not_on_the_map()
    {
        // Not "at 0,0" — off the map entirely. A null island marker is worse
        // than an absent one, because somebody has to work out it is fake.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var map = await MapAsync(new GetCurrentLocationsQuery());

        Assert.DoesNotContain(map, l => l.EmployeeId == employee.Id);
    }

    [Fact]
    public async Task An_employee_whose_last_ping_is_older_than_the_window_drops_off_entirely()
    {
        // The subtle half of MaxAgeMinutes. The cutoff filters the pings, and
        // an employee left with none is then dropped — rather than shown at
        // their last known position, which is how a map ends up displaying
        // somebody who went home four hours ago as if they were still on site.
        var (stale, staleUser) = await SeedWorkerAsync();
        var (fresh, freshUser) = await SeedWorkerAsync();

        await ReportAsync(staleUser, stale.Id, Ping(Noon.AddHours(-3)));
        await ReportAsync(freshUser, fresh.Id, Ping(Noon.AddMinutes(-5)));

        var map = await InScope(async scope =>
        {
            scope.Clock.FreezeAt(Noon);

            var page = await scope.Send(new GetCurrentLocationsQuery
            {
                MaxAgeMinutes = 30,
                PageSize = GetCurrentLocationsQuery.MaxPageSize
            });

            return page.Items;
        });

        Assert.Contains(map, l => l.EmployeeId == fresh.Id);
        Assert.DoesNotContain(map, l => l.EmployeeId == stale.Id);
    }

    [Fact]
    public async Task An_old_ping_does_not_hide_a_recent_one_inside_the_window()
    {
        // The other order of the same rule: filtering happens before "newest",
        // so an employee with one stale and one fresh ping must still appear,
        // at the fresh position.
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(
            user,
            employee.Id,
            Ping(Noon.AddHours(-3), lat: 44.00),
            Ping(Noon.AddMinutes(-5), lat: 46.00));

        var map = await InScope(async scope =>
        {
            scope.Clock.FreezeAt(Noon);

            var page = await scope.Send(new GetCurrentLocationsQuery
            {
                MaxAgeMinutes = 30,
                PageSize = GetCurrentLocationsQuery.MaxPageSize
            });

            return page.Items;
        });

        var mine = Assert.Single(map, l => l.EmployeeId == employee.Id);
        Assert.Equal(46.00, mine.Latitude, 5);
    }

    [Fact]
    public async Task The_map_leaves_out_employees_who_have_left_unless_asked()
    {
        var (active, activeUser) = await SeedWorkerAsync();
        var (former, formerUser) = await SeedWorkerAsync(EmployeeStatus.Terminated);

        await ReportAsync(activeUser, active.Id, Ping(Noon));
        await ReportAsync(formerUser, former.Id, Ping(Noon));

        var normal = await MapAsync(new GetCurrentLocationsQuery());

        Assert.Contains(normal, l => l.EmployeeId == active.Id);
        Assert.DoesNotContain(normal, l => l.EmployeeId == former.Id);

        var everyone = await MapAsync(new GetCurrentLocationsQuery { IncludeInactive = true });

        Assert.Contains(everyone, l => l.EmployeeId == former.Id);
    }

    [Fact]
    public async Task The_map_can_be_narrowed_to_one_project_crew()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        var (onSite, onSiteUser) = await SeedWorkerAsync();
        var (elsewhere, elsewhereUser) = await SeedWorkerAsync();

        await InScope(scope =>
            scope.Send(new AssignEmployeeToProjectCommand(onSite.Id, project.Id)));

        await ReportAsync(onSiteUser, onSite.Id, Ping(Noon));
        await ReportAsync(elsewhereUser, elsewhere.Id, Ping(Noon));

        var crew = await MapAsync(new GetCurrentLocationsQuery { ProjectId = project.Id });

        Assert.Contains(crew, l => l.EmployeeId == onSite.Id);
        Assert.DoesNotContain(crew, l => l.EmployeeId == elsewhere.Id);
    }

    [Fact]
    public async Task A_deleted_employee_disappears_from_the_map_without_their_pings_being_touched()
    {
        // Two filters, not one. The employee vanishes by the soft-delete
        // filter; the pings vanish by their own — `location_records` is
        // filtered on `!l.Employee.IsDeleted`, so they go at the same moment
        // without anything cascading.
        //
        // The rows themselves stay, which is the part worth pinning. A delete
        // that took the history with it would make the deletion irreversible
        // and would quietly remove evidence that the retention policy is meant
        // to age out on its own schedule.
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(user, employee.Id, Ping(Noon));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        var map = await MapAsync(new GetCurrentLocationsQuery());
        Assert.DoesNotContain(map, l => l.EmployeeId == employee.Id);

        var visible = await InScope(scope => scope.Db.LocationRecords
            .CountAsync(l => l.EmployeeId == employee.Id));
        Assert.Equal(0, visible);

        var onDisk = await InScope(scope => scope.Db.LocationRecords
            .IgnoreQueryFilters()
            .CountAsync(l => l.EmployeeId == employee.Id));
        Assert.Equal(1, onDisk);
    }

    [Fact]
    public async Task The_map_carries_enough_to_label_a_marker()
    {
        // The admin map draws a pin per employee and needs a name on it. If
        // the join to the employee were dropped the query would still return
        // rows, just anonymous ones.
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(user, employee.Id, Ping(Noon));

        var map = await MapAsync(new GetCurrentLocationsQuery());
        var mine = Assert.Single(map, l => l.EmployeeId == employee.Id);

        Assert.Equal(employee.EmployeeNumber, mine.EmployeeNumber);
        Assert.Equal($"{employee.FirstName} {employee.LastName}", mine.FullName);
        Assert.Equal(employee.Position, mine.Position);
    }

    // ---- the map is bounded ----------------------------------------------

    [Fact]
    public async Task The_map_is_bounded_even_when_nobody_asks_for_a_page()
    {
        // The point of M12. This used to return every active employee who had
        // ever reported, with no limit — fine at the stated scale and a
        // response that grows without bound for anybody larger.
        var page = await InScope(scope => scope.Send(new GetCurrentLocationsQuery()));

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(250, page.PageSize);
        Assert.True(page.Items.Count <= 250);
    }

    [Fact]
    public async Task The_map_says_how_many_there_are_so_a_partial_one_can_be_spotted()
    {
        // A grid has a scrollbar; a map has nothing to hint that a marker is
        // missing. TotalCount is what lets the client say "showing 250 of 400"
        // instead of quietly drawing the wrong picture.
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        for (var i = 0; i < 3; i++)
        {
            var (employee, user) = await SeedWorkerAsync();

            await InScope(scope =>
                scope.Send(new AssignEmployeeToProjectCommand(employee.Id, project.Id)));

            await ReportAsync(user, employee.Id, Ping(Noon));
        }

        var firstOfThree = await InScope(scope => scope.Send(new GetCurrentLocationsQuery
        {
            ProjectId = project.Id,
            PageSize = 1
        }));

        Assert.Single(firstOfThree.Items);
        Assert.Equal(3, firstOfThree.TotalCount);
        Assert.True(firstOfThree.HasNextPage);
    }

    [Fact]
    public async Task Paging_the_map_neither_repeats_nor_drops_a_marker()
    {
        // What this proves: the paging arithmetic. Three pages of two plus a
        // remainder cover all seven exactly once, with no off-by-one at either
        // end and nothing lost in the last partial page.
        //
        // What it does not prove, despite the shape of it: that the query's
        // unique tiebreaker is present. Removing `.ThenBy(EmployeeId)` leaves
        // this green — three runs out of three — because PostgreSQL returns
        // seven rows in a stable order regardless, and the instability the
        // tiebreaker exists for only appears once the planner has a reason to
        // choose differently between two queries. Said plainly here rather
        // than left to look like a guard it is not.
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        var seeded = new List<Guid>();

        for (var i = 0; i < 7; i++)
        {
            // Same name for all of them, so they sort equally and only the
            // tiebreaker separates them.
            var employee = await InScope(scope =>
                TestData.SeedEmployeeAsync(scope, firstName: "Same", lastName: "Mapname"));
            var user = await InScope(scope =>
                TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

            await InScope(scope =>
                scope.Send(new AssignEmployeeToProjectCommand(employee.Id, project.Id)));

            await ReportAsync(user, employee.Id, Ping(Noon));

            seeded.Add(employee.Id);
        }

        var seen = new List<Guid>();

        for (var page = 1; page <= 4; page++)
        {
            var result = await InScope(scope => scope.Send(new GetCurrentLocationsQuery
            {
                ProjectId = project.Id,
                PageNumber = page,
                PageSize = 2
            }));

            seen.AddRange(result.Items.Select(i => i.EmployeeId));
        }

        Assert.Equal(7, seen.Count);
        Assert.Equal(7, seen.Distinct().Count());
        Assert.All(seeded, id => Assert.Contains(id, seen));
    }

    [Fact]
    public async Task A_page_larger_than_the_ceiling_is_refused()
    {
        // Otherwise the bound is advisory: a client that wants everything
        // simply asks for everything, and the endpoint is unbounded again with
        // extra steps.
        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
            scope.Send(new GetCurrentLocationsQuery
            {
                PageSize = GetCurrentLocationsQuery.MaxPageSize + 1
            })));
    }

    [Fact]
    public async Task The_ceiling_is_high_enough_to_draw_a_whole_site_in_one_request()
    {
        // A guard on the decision rather than on behaviour. Dropping this to a
        // grid's twenty would make the map take dozens of round trips, and the
        // temptation would then be to remove the bound rather than raise it.
        Assert.True(GetCurrentLocationsQuery.MaxPageSize >= 1_000);
    }

    // ---- one employee ----------------------------------------------------

    [Fact]
    public async Task The_last_known_position_is_the_newest_one()
    {
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(
            user,
            employee.Id,
            Ping(Noon.AddMinutes(5), lat: 45.05),
            Ping(Noon.AddMinutes(45), lat: 45.45),
            Ping(Noon.AddMinutes(25), lat: 45.25));

        var last = await InScope(scope =>
            scope.Send(new GetEmployeeLastLocationQuery(employee.Id)));

        Assert.Equal(45.45, last.Latitude, 5);
        Assert.Equal(Noon.AddMinutes(45), last.Timestamp);
    }

    [Fact]
    public async Task Asking_where_a_stranger_is_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetEmployeeLastLocationQuery(Guid.NewGuid()))));
    }

    [Fact]
    public async Task Asking_where_somebody_is_before_they_have_reported_reports_not_found()
    {
        // Same exception type as an unknown employee but a different message,
        // because the remedy differs: one is a bad id, the other is a phone
        // that has not sent anything yet.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var error = await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetEmployeeLastLocationQuery(employee.Id))));

        Assert.Contains(employee.Id.ToString(), error.Message);
        Assert.Contains("location", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- history ---------------------------------------------------------

    [Fact]
    public async Task History_comes_back_newest_first()
    {
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(
            user,
            employee.Id,
            Ping(Noon),
            Ping(Noon.AddMinutes(20)),
            Ping(Noon.AddMinutes(10)));

        var history = await InScope(scope =>
            scope.Send(new GetLocationHistoryQuery { EmployeeId = employee.Id }));

        Assert.Equal(
            [Noon.AddMinutes(20), Noon.AddMinutes(10), Noon],
            history.Items.Select(i => i.Timestamp));
    }

    [Fact]
    public async Task History_can_be_limited_to_a_window_and_both_ends_are_inclusive()
    {
        // Route playback asks for a shift. An exclusive bound silently drops
        // the ping at the exact clock-in minute, which is the one that says
        // where the shift started.
        var (employee, user) = await SeedWorkerAsync();

        await ReportAsync(
            user,
            employee.Id,
            Ping(Noon.AddHours(-1)),
            Ping(Noon),
            Ping(Noon.AddHours(1)),
            Ping(Noon.AddHours(2)));

        var window = await InScope(scope => scope.Send(new GetLocationHistoryQuery
        {
            EmployeeId = employee.Id,
            From = Noon,
            To = Noon.AddHours(1)
        }));

        Assert.Equal(
            [Noon.AddHours(1), Noon],
            window.Items.Select(i => i.Timestamp));
    }

    [Fact]
    public async Task History_pages_without_repeating_or_skipping_a_ping()
    {
        var (employee, user) = await SeedWorkerAsync();

        var batch = Enumerable.Range(0, 25)
            .Select(minute => Ping(Noon.AddMinutes(minute)))
            .ToArray();

        await ReportAsync(user, employee.Id, batch);

        var first = await InScope(scope => scope.Send(new GetLocationHistoryQuery
        {
            EmployeeId = employee.Id,
            PageNumber = 1,
            PageSize = 10
        }));

        var second = await InScope(scope => scope.Send(new GetLocationHistoryQuery
        {
            EmployeeId = employee.Id,
            PageNumber = 2,
            PageSize = 10
        }));

        var third = await InScope(scope => scope.Send(new GetLocationHistoryQuery
        {
            EmployeeId = employee.Id,
            PageNumber = 3,
            PageSize = 10
        }));

        Assert.Equal(25, first.TotalCount);
        Assert.Equal(5, third.Items.Count);

        var seen = first.Items.Concat(second.Items).Concat(third.Items)
            .Select(i => i.Id)
            .ToList();

        Assert.Equal(25, seen.Distinct().Count());
    }

    [Fact]
    public async Task History_only_ever_covers_the_employee_that_was_asked_for()
    {
        var (mine, myUser) = await SeedWorkerAsync();
        var (theirs, theirUser) = await SeedWorkerAsync();

        await ReportAsync(myUser, mine.Id, Ping(Noon));
        await ReportAsync(theirUser, theirs.Id, Ping(Noon));

        var history = await InScope(scope =>
            scope.Send(new GetLocationHistoryQuery { EmployeeId = mine.Id }));

        Assert.All(history.Items, item => Assert.Equal(mine.Id, item.EmployeeId));
    }

    [Fact]
    public async Task Asking_for_a_stranger_s_history_reports_not_found()
    {
        // Rather than an empty page, which reads as "this employee went
        // nowhere" instead of "there is no such employee".
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
            scope.Send(new GetLocationHistoryQuery { EmployeeId = Guid.NewGuid() })));
    }
}
