using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Employees.Commands.CreateEmployee;
using Construction.Application.Features.Employees.Queries.GetEmployeeById;
using Construction.Application.Features.Employees.Queries.GetEmployees;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Which business unit employs a person, and since when: moving someone closes the stretch they
/// were in the day before, history never overlaps, and a date that does not fit it is refused
/// instead of rewriting which unit paid for the hours in between.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class EmployeeBranchTests : IntegrationTestBase
{
    public EmployeeBranchTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly Today = new(2026, 10, 2);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<T> InFrozenScope<T>(Func<TestScope, Task<T>> action, UserRole role = UserRole.SuperAdmin)
    {
        using var scope = Fixture.CreateScope();
        scope.Clock.FreezeAt(new DateTime(Today.Year, Today.Month, Today.Day, 10, 0, 0, DateTimeKind.Utc));
        var user = await TestData.SeedUserAsync(scope, role);
        scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);

        return await action(scope);
    }

    private Task<BranchDto> NewBranchAsync() => InFrozenScope(scope =>
        scope.Send(new CreateBranchCommand { Name = $"Unit {Unique()}", Color = "#3457D5" }));

    private async Task<Employee> NewEmployeeAsync()
    {
        using var scope = Fixture.CreateScope();

        return await TestData.SeedEmployeeAsync(scope);
    }

    private Task<IReadOnlyList<EmployeeBranchPeriodDto>> MoveAsync(Guid employeeId, Guid? branchId, DateOnly? from = null) =>
        InFrozenScope(scope => scope.Send(new SetEmployeeBranchCommand { EmployeeId = employeeId, BranchId = branchId, From = from }));

    [Fact]
    public async Task An_employee_is_put_in_a_unit_from_a_date()
    {
        var unit = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        var history = await MoveAsync(employee.Id, unit.Id, new DateOnly(2026, 3, 1));

        var period = Assert.Single(history);
        Assert.Equal(unit.Id, period.BranchId);
        Assert.Equal(new DateOnly(2026, 3, 1), period.StartDate);
        Assert.Null(period.EndDate);
    }

    [Fact]
    public async Task Moving_to_another_unit_closes_the_stretch_the_day_before()
    {
        var first = await NewBranchAsync();
        var second = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await MoveAsync(employee.Id, first.Id, new DateOnly(2026, 1, 1));
        var history = await MoveAsync(employee.Id, second.Id, new DateOnly(2026, 6, 1));

        Assert.Equal(2, history.Count);
        var current = history[0];
        var previous = history[1];

        Assert.Equal(second.Id, current.BranchId);
        Assert.Null(current.EndDate);
        Assert.Equal(first.Id, previous.BranchId);
        Assert.Equal(new DateOnly(2026, 5, 31), previous.EndDate);
    }

    [Fact]
    public async Task Moving_to_the_unit_they_are_already_in_changes_nothing()
    {
        var unit = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await MoveAsync(employee.Id, unit.Id, new DateOnly(2026, 1, 1));
        var history = await MoveAsync(employee.Id, unit.Id, new DateOnly(2026, 8, 1));

        Assert.Single(history);
        Assert.Equal(new DateOnly(2026, 1, 1), history[0].StartDate);
    }

    [Fact]
    public async Task Leaving_every_unit_ends_the_stretch_and_opens_none()
    {
        var unit = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await MoveAsync(employee.Id, unit.Id, new DateOnly(2026, 1, 1));
        var history = await MoveAsync(employee.Id, null, new DateOnly(2026, 9, 1));

        var period = Assert.Single(history);
        Assert.Equal(new DateOnly(2026, 8, 31), period.EndDate);
    }

    [Fact]
    public async Task A_date_before_the_current_stretch_began_is_refused()
    {
        var first = await NewBranchAsync();
        var second = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await MoveAsync(employee.Id, first.Id, new DateOnly(2026, 6, 1));

        await Assert.ThrowsAsync<ConflictException>(() => MoveAsync(employee.Id, second.Id, new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public async Task A_date_that_overlaps_an_earlier_closed_stretch_is_refused()
    {
        var a = await NewBranchAsync();
        var b = await NewBranchAsync();
        var c = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await MoveAsync(employee.Id, a.Id, new DateOnly(2026, 1, 1));
        await MoveAsync(employee.Id, b.Id, new DateOnly(2026, 4, 1));

        // The A stretch ran to 31 March; opening C in the middle of it would rewrite those hours.
        await Assert.ThrowsAsync<ConflictException>(() => MoveAsync(employee.Id, c.Id, new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public async Task A_move_cannot_be_dated_in_the_future()
    {
        var unit = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await Assert.ThrowsAsync<ConflictException>(() => MoveAsync(employee.Id, unit.Id, Today.AddDays(1)));
    }

    [Fact]
    public async Task A_correction_on_the_day_a_stretch_began_replaces_the_unit_instead_of_splitting_it()
    {
        var wrong = await NewBranchAsync();
        var right = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await MoveAsync(employee.Id, wrong.Id, new DateOnly(2026, 5, 1));
        var history = await MoveAsync(employee.Id, right.Id, new DateOnly(2026, 5, 1));

        var period = Assert.Single(history);
        Assert.Equal(right.Id, period.BranchId);
    }

    [Fact]
    public async Task A_wrong_period_can_be_removed_and_then_the_date_fits()
    {
        var a = await NewBranchAsync();
        var b = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await MoveAsync(employee.Id, a.Id, new DateOnly(2026, 6, 1));
        var wrong = Assert.Single(await MoveAsync(employee.Id, b.Id, new DateOnly(2026, 8, 1)), p => p.EndDate is null);

        var afterRemoval = await InFrozenScope(scope => scope.Send(new RemoveEmployeeBranchPeriodCommand(employee.Id, wrong.Id)));

        Assert.Single(afterRemoval);
        Assert.Equal(a.Id, afterRemoval[0].BranchId);
    }

    [Fact]
    public async Task Only_management_moves_people_between_units()
    {
        var unit = await NewBranchAsync();
        var employee = await NewEmployeeAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InFrozenScope(
            scope => scope.Send(new SetEmployeeBranchCommand { EmployeeId = employee.Id, BranchId = unit.Id }),
            UserRole.Foreman));
    }

    [Fact]
    public async Task Several_employees_move_into_a_unit_together_and_one_that_does_not_fit_stops_them_all()
    {
        var unit = await NewBranchAsync();
        var other = await NewBranchAsync();
        var one = await NewEmployeeAsync();
        var two = await NewEmployeeAsync();
        var late = await NewEmployeeAsync();

        // 'late' has been in another unit since 1 September, so a move dated 1 August cannot fit.
        await MoveAsync(late.Id, other.Id, new DateOnly(2026, 9, 1));

        await Assert.ThrowsAsync<ConflictException>(() => InFrozenScope(scope => scope.Send(
            new AssignEmployeesToBranchCommand { Id = unit.Id, EmployeeIds = [one.Id, two.Id, late.Id], From = new DateOnly(2026, 8, 1) })));

        using (var check = Fixture.CreateScope())
        {
            Assert.DoesNotContain(check.Db.EmployeeBranches, p => p.EmployeeId == one.Id || p.EmployeeId == two.Id);
        }

        var result = await InFrozenScope(scope => scope.Send(
            new AssignEmployeesToBranchCommand { Id = unit.Id, EmployeeIds = [one.Id, two.Id], From = new DateOnly(2026, 8, 1) }));

        Assert.Equal(2, result.EmployeeCount);
    }

    [Fact]
    public async Task The_employee_list_and_detail_say_which_unit_employs_them_and_can_be_narrowed_to_it()
    {
        var unit = await NewBranchAsync();
        var member = await NewEmployeeAsync();
        var outsider = await NewEmployeeAsync();

        await MoveAsync(member.Id, unit.Id, new DateOnly(2026, 2, 1));

        var page = await InFrozenScope(scope => scope.Send(new GetEmployeesQuery { BranchId = unit.Id, PageSize = 100 }));

        var row = Assert.Single(page.Items);
        Assert.Equal(member.Id, row.Id);
        Assert.Equal(unit.Id, row.BranchId);
        Assert.Equal(unit.Name, row.BranchName);
        Assert.DoesNotContain(page.Items, e => e.Id == outsider.Id);

        var detail = await InFrozenScope(scope => scope.Send(new GetEmployeeByIdQuery(member.Id)));
        Assert.Equal(unit.Id, detail.BranchId);
        Assert.Single(detail.BranchHistory);
    }

    [Fact]
    public async Task A_new_hire_can_be_placed_in_a_unit_from_their_employment_date()
    {
        var unit = await NewBranchAsync();

        var created = await InFrozenScope(scope => scope.Send(new CreateEmployeeCommand
        {
            EmployeeNumber = $"E{Unique()}",
            FirstName = "Nova",
            LastName = "Osoba",
            Position = "Zidar",
            EmploymentDate = new DateOnly(2026, 9, 15),
            Status = EmployeeStatus.Active,
            Type = EmployeeType.Employee,
            BranchId = unit.Id,
        }));

        var detail = await InFrozenScope(scope => scope.Send(new GetEmployeeByIdQuery(created.Id)));
        Assert.Equal(unit.Id, detail.BranchId);

        using var check = Fixture.CreateScope();
        var period = check.Db.EmployeeBranches.Single(p => p.EmployeeId == created.Id);
        Assert.Equal(new DateOnly(2026, 9, 15), period.StartDate);
    }

    [Fact]
    public async Task A_unit_with_employees_reports_how_many_it_employs_now()
    {
        var unit = await NewBranchAsync();
        var one = await NewEmployeeAsync();
        var two = await NewEmployeeAsync();

        await MoveAsync(one.Id, unit.Id, new DateOnly(2026, 1, 1));
        await MoveAsync(two.Id, unit.Id, new DateOnly(2026, 1, 1));
        await MoveAsync(two.Id, null, new DateOnly(2026, 7, 1));

        var listed = (await InFrozenScope(scope => scope.Send(new GetBranchesQuery()))).Single(b => b.Id == unit.Id);

        Assert.Equal(1, listed.EmployeeCount);
    }

    [Fact]
    public async Task Bringing_newcomers_in_can_start_each_from_their_own_employment_date()
    {
        var unit = await NewBranchAsync();
        var other = await NewBranchAsync();
        var newcomer = await NewEmployeeAsync();
        var veteran = await NewEmployeeAsync();

        // The veteran already has a unit, so the "from employment date" rule does not apply to them.
        await MoveAsync(veteran.Id, other.Id, new DateOnly(2026, 2, 1));

        await InFrozenScope(scope => scope.Send(new AssignEmployeesToBranchCommand
        {
            Id = unit.Id,
            EmployeeIds = [newcomer.Id, veteran.Id],
            From = new DateOnly(2026, 9, 1),
            BackdateNewcomers = true,
        }));

        using var check = Fixture.CreateScope();
        var hired = check.Db.Employees.Single(e => e.Id == newcomer.Id).EmploymentDate;

        var first = check.Db.EmployeeBranches.Single(p => p.EmployeeId == newcomer.Id);
        Assert.Equal(hired < Today ? hired : Today, first.StartDate);

        var moved = check.Db.EmployeeBranches.Single(p => p.EmployeeId == veteran.Id && p.BranchId == unit.Id);
        Assert.Equal(new DateOnly(2026, 9, 1), moved.StartDate);
    }
}
