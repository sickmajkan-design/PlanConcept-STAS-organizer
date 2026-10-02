using Construction.Application.Features.Absences.Queries.GetAbsences;
using Construction.Application.Features.Absences.Queries.GetSchedule;
using Construction.Application.Features.Accommodations.Queries.GetAccommodations;
using Construction.Application.Features.Assignments.Queries.GetAssignmentBoard;
using Construction.Application.Features.Employees.Queries.GetOrganizationHierarchy;
using Construction.Application.Features.Locations.Queries.GetCurrentLocations;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The people-centred screens follow the unit chosen in the header: leave, the schedule, the
/// assignment board, the live map, the org chart — each shows the people that unit employs, and
/// accommodation shows the housing booked to it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BranchPeopleScopeTests : IntegrationTestBase
{
    public BranchPeopleScopeTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private sealed record World(Branch Unit, Employee Inside, Employee Outside);

    private Task<World> SeedAsync() => InScope(async scope =>
    {
        var unit = new Branch { Name = $"People {Unique()}", Color = "#3457D5" };
        scope.Db.Branches.Add(unit);

        var inside = await TestData.SeedEmployeeAsync(scope);
        var outside = await TestData.SeedEmployeeAsync(scope);
        await scope.Db.SaveChangesAsync();

        scope.Db.EmployeeBranches.Add(new EmployeeBranch { EmployeeId = inside.Id, BranchId = unit.Id, StartDate = new DateOnly(2026, 1, 1) });
        await scope.Db.SaveChangesAsync();

        return new World(unit, inside, outside);
    });

    private async Task<T> AsSuperAdmin<T>(Func<TestScope, Task<T>> action) => await InScope(async scope =>
    {
        var admin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
        scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);

        return await action(scope);
    });

    [Fact]
    public async Task Leave_requests_of_one_unit_only()
    {
        var world = await SeedAsync();

        await InScope(async scope =>
        {
            foreach (var person in new[] { world.Inside, world.Outside })
            {
                scope.Db.Absences.Add(new Absence
                {
                    EmployeeId = person.Id,
                    Type = AbsenceType.AnnualLeave,
                    Status = AbsenceStatus.Approved,
                    StartDate = new DateOnly(2026, 8, 3),
                    EndDate = new DateOnly(2026, 8, 7),
                });
            }

            await scope.Db.SaveChangesAsync();
        });

        var page = await AsSuperAdmin(scope => scope.Send(new GetAbsencesQuery { BranchId = world.Unit.Id, PageSize = 100 }));

        Assert.Equal([world.Inside.Id], page.Items.Select(a => a.EmployeeId).ToArray());
    }

    [Fact]
    public async Task Leave_follows_the_unit_that_employed_the_person_on_the_day_it_began()
    {
        var first = await SeedAsync();
        var second = new Branch { Name = $"People {Unique()}", Color = "#0F8A5F" };

        await InScope(async scope =>
        {
            scope.Db.Branches.Add(second);

            // Left the first unit on 30 June and joined the second on 1 July.
            var period = scope.Db.EmployeeBranches.Single(p => p.EmployeeId == first.Inside.Id);
            period.EndDate = new DateOnly(2026, 6, 30);
            scope.Db.EmployeeBranches.Add(new EmployeeBranch { EmployeeId = first.Inside.Id, BranchId = second.Id, StartDate = new DateOnly(2026, 7, 1) });

            scope.Db.Absences.AddRange(
                new Absence { EmployeeId = first.Inside.Id, Type = AbsenceType.AnnualLeave, Status = AbsenceStatus.Approved, StartDate = new DateOnly(2026, 3, 2), EndDate = new DateOnly(2026, 3, 6) },
                new Absence { EmployeeId = first.Inside.Id, Type = AbsenceType.AnnualLeave, Status = AbsenceStatus.Approved, StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 7) });

            await scope.Db.SaveChangesAsync();
        });

        var inFirst = await AsSuperAdmin(scope => scope.Send(new GetAbsencesQuery { BranchId = first.Unit.Id, EmployeeId = first.Inside.Id, PageSize = 100 }));
        var inSecond = await AsSuperAdmin(scope => scope.Send(new GetAbsencesQuery { BranchId = second.Id, EmployeeId = first.Inside.Id, PageSize = 100 }));

        Assert.Equal(new DateOnly(2026, 3, 2), Assert.Single(inFirst.Items).StartDate);
        Assert.Equal(new DateOnly(2026, 8, 3), Assert.Single(inSecond.Items).StartDate);
    }

    [Fact]
    public async Task The_schedule_holds_the_people_the_unit_employed_in_the_window()
    {
        var world = await SeedAsync();

        var board = await AsSuperAdmin(scope => scope.Send(new GetScheduleQuery
        {
            From = new DateOnly(2026, 9, 7),
            To = new DateOnly(2026, 9, 13),
            BranchId = world.Unit.Id,
        }));

        Assert.Contains(board.Rows, r => r.EmployeeId == world.Inside.Id);
        Assert.DoesNotContain(board.Rows, r => r.EmployeeId == world.Outside.Id);
    }

    [Fact]
    public async Task The_assignment_board_lists_only_the_units_employees()
    {
        var world = await SeedAsync();

        var board = await AsSuperAdmin(scope => scope.Send(new GetAssignmentBoardQuery { BranchId = world.Unit.Id }));

        Assert.Equal([world.Inside.Id], board.Employees.Select(e => e.Id).ToArray());
    }

    [Fact]
    public async Task The_org_chart_and_the_live_map_narrow_the_same_way()
    {
        var world = await SeedAsync();

        await InScope(async scope =>
        {
            foreach (var person in new[] { world.Inside, world.Outside })
            {
                scope.Db.LocationRecords.Add(new LocationRecord
                {
                    EmployeeId = person.Id,
                    Latitude = 43.85,
                    Longitude = 18.4,
                    Timestamp = DateTime.UtcNow,
                });
            }

            await scope.Db.SaveChangesAsync();
        });

        var chart = await AsSuperAdmin(scope => scope.Send(new GetOrganizationHierarchyQuery { BranchId = world.Unit.Id }));
        Assert.Equal([world.Inside.Id], chart.People.Select(p => p.EmployeeId).ToArray());

        var map = await AsSuperAdmin(scope => scope.Send(new GetCurrentLocationsQuery { BranchId = world.Unit.Id, PageSize = 100 }));
        Assert.Equal([world.Inside.Id], map.Items.Select(l => l.EmployeeId).ToArray());
    }

    [Fact]
    public async Task Housing_booked_to_a_unit_is_listed_under_it()
    {
        var world = await SeedAsync();

        var housing = await InScope(async scope =>
        {
            var mine = new Accommodation { Address = $"Ulica {Unique()}", BranchId = world.Unit.Id };
            var other = new Accommodation { Address = $"Ulica {Unique()}" };
            scope.Db.Accommodations.AddRange(mine, other);
            await scope.Db.SaveChangesAsync();

            return mine;
        });

        var page = await AsSuperAdmin(scope => scope.Send(new GetAccommodationsQuery { BranchId = world.Unit.Id, PageSize = 100 }));

        Assert.Equal([housing.Id], page.Items.Select(a => a.Id).ToArray());
    }
}
