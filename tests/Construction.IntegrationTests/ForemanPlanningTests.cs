using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Planning;
using Construction.Application.Features.Users.Commands.UpdateUser;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A foreman sees the schedule of their own business unit and nothing beyond it, and moves people only
/// when an administrator has given them that right, and then only inside that unit.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ForemanPlanningTests : IntegrationTestBase
{
    public ForemanPlanningTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly D(int day) => new(2031, 7, day);

    private async Task<Branch> NewBranchAsync() =>
        await InScope(async scope =>
        {
            var branch = new Branch { Name = $"Unit {Guid.NewGuid():N}"[..14] };
            scope.Db.Branches.Add(branch);
            await scope.Db.SaveChangesAsync();
            return branch;
        });

    private Task PlaceAsync(Guid employeeId, Guid branchId) =>
        InScope(async scope =>
        {
            scope.Db.EmployeeBranches.Add(new EmployeeBranch { EmployeeId = employeeId, BranchId = branchId, StartDate = new DateOnly(2030, 1, 1) });
            await scope.Db.SaveChangesAsync();
        });

    private Task SetProjectBranchAsync(Guid projectId, Guid branchId) =>
        InScope(async scope =>
        {
            var project = await scope.Db.Projects.SingleAsync(p => p.Id == projectId);
            project.BranchId = branchId;
            await scope.Db.SaveChangesAsync();
        });

    /// <summary>A foreman employed in <paramref name="branch"/>, with or without the right to plan.</summary>
    private async Task<(User User, Employee Employee)> ForemanAsync(Branch branch, bool canPlan)
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await PlaceAsync(employee.Id, branch.Id);

        var user = await InScope(async scope =>
        {
            var created = await TestData.SeedUserAsync(scope, UserRole.Foreman, employee.Id);
            created.CanPlan = canPlan;
            await scope.Db.SaveChangesAsync();
            return created;
        });

        return (user, employee);
    }

    private Task<T> AsForeman<T>((User User, Employee Employee) foreman, Func<TestScope, Task<T>> action) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(foreman.User.Id, UserRole.Foreman, foreman.Employee.Id, foreman.User.Email);
            return action(scope);
        });

    private Task Do<TRequest>((User User, Employee Employee) foreman, TRequest command) where TRequest : MediatR.IRequest =>
        AsForeman(foreman, async scope =>
        {
            await scope.Send(command);
            return 0;
        });

    private Task<PlanningDto> PlanAsync((User User, Employee Employee) foreman) =>
        AsForeman(foreman, scope => scope.Send(new GetPlanningQuery { From = D(1), To = D(30) }));

    [Fact]
    public async Task A_foreman_sees_only_their_own_unit_and_cannot_widen_it_by_asking_for_another()
    {
        var mine = await NewBranchAsync();
        var theirs = await NewBranchAsync();
        var foreman = await ForemanAsync(mine, canPlan: false);
        var colleague = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var stranger = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await PlaceAsync(colleague.Id, mine.Id);
        await PlaceAsync(stranger.Id, theirs.Id);

        var plan = await AsForeman(foreman, scope => scope.Send(new GetPlanningQuery { From = D(1), To = D(30), BranchId = theirs.Id }));

        Assert.Contains(plan.Employees, e => e.Id == colleague.Id);
        Assert.DoesNotContain(plan.Employees, e => e.Id == stranger.Id);
        Assert.True(plan.IsScoped);
        Assert.False(plan.CanEdit);
        Assert.Equal(mine.Name, plan.ScopeBranchName);
    }

    [Fact]
    public async Task A_foreman_in_no_unit_sees_nobody()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var user = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman, employee.Id));

        var plan = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(user.Id, UserRole.Foreman, employee.Id, user.Email);
            return scope.Send(new GetPlanningQuery { From = D(1), To = D(30) });
        });

        Assert.Empty(plan.Employees);
        Assert.True(plan.IsScoped);
    }

    [Fact]
    public async Task A_foreman_without_the_right_cannot_move_anybody_not_even_their_own_people()
    {
        var unit = await NewBranchAsync();
        var foreman = await ForemanAsync(unit, canPlan: false);
        var colleague = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await PlaceAsync(colleague.Id, unit.Id);
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        await SetProjectBranchAsync(site.Id, unit.Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Do(foreman, new SetEmployeeScheduleCommand { EmployeeId = colleague.Id, ProjectId = site.Id, From = D(1), To = D(5) }));
    }

    [Fact]
    public async Task A_foreman_with_the_right_moves_people_of_their_unit_between_sites_of_their_unit()
    {
        var unit = await NewBranchAsync();
        var foreman = await ForemanAsync(unit, canPlan: true);
        var colleague = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await PlaceAsync(colleague.Id, unit.Id);
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        await SetProjectBranchAsync(site.Id, unit.Id);

        await AsForeman(foreman, async scope =>
        {
            await scope.Send(new SetEmployeeScheduleCommand { EmployeeId = colleague.Id, ProjectId = site.Id, From = D(1), To = D(5) });
            return 0;
        });

        var posted = await InScope(scope => scope.Db.EmployeeProjects.CountAsync(ep => ep.EmployeeId == colleague.Id && ep.ProjectId == site.Id));
        Assert.Equal(1, posted);
    }

    [Fact]
    public async Task The_right_stops_at_the_edge_of_the_unit()
    {
        var unit = await NewBranchAsync();
        var other = await NewBranchAsync();
        var foreman = await ForemanAsync(unit, canPlan: true);
        var colleague = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await PlaceAsync(colleague.Id, unit.Id);
        var stranger = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await PlaceAsync(stranger.Id, other.Id);
        var ownSite = await InScope(scope => TestData.SeedProjectAsync(scope));
        await SetProjectBranchAsync(ownSite.Id, unit.Id);
        var foreignSite = await InScope(scope => TestData.SeedProjectAsync(scope));
        await SetProjectBranchAsync(foreignSite.Id, other.Id);

        // Somebody of another unit.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Do(foreman, new SetEmployeeScheduleCommand { EmployeeId = stranger.Id, ProjectId = ownSite.Id, From = D(1), To = D(5) }));

        // Own person, another unit site.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Do(foreman, new SetEmployeeScheduleCommand { EmployeeId = colleague.Id, ProjectId = foreignSite.Id, From = D(1), To = D(5) }));

        // A swap with somebody of another unit.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Do(foreman, new SwapEmployeeSchedulesCommand { EmployeeAId = colleague.Id, EmployeeBId = stranger.Id, From = D(1), To = D(5) }));
    }

    [Fact]
    public async Task What_a_project_needs_is_never_a_foremans_to_change()
    {
        var unit = await NewBranchAsync();
        var foreman = await ForemanAsync(unit, canPlan: true);
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        await SetProjectBranchAsync(site.Id, unit.Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Do(foreman, new SetProjectStaffingNeedsCommand { ProjectId = site.Id, Needs = [new("Zidar", 1)] }));
    }

    [Fact]
    public async Task A_foreman_is_not_told_what_kind_of_leave_somebody_has_only_that_they_are_away()
    {
        var unit = await NewBranchAsync();
        var foreman = await ForemanAsync(unit, canPlan: false);
        var colleague = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await PlaceAsync(colleague.Id, unit.Id);

        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence { EmployeeId = colleague.Id, Type = AbsenceType.SickLeave, Status = AbsenceStatus.Approved, StartDate = D(3), EndDate = D(4) });
            await scope.Db.SaveChangesAsync();
        });

        var asForeman = await PlanAsync(foreman);
        var asManager = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(Guid.NewGuid(), UserRole.ProjectManager, null, "pm@example.com");
            return scope.Send(new GetPlanningQuery { From = D(1), To = D(30) });
        });

        Assert.Equal("Other", asForeman.Employees.Single(e => e.Id == colleague.Id).Absences.Single().Type);
        Assert.Equal("SickLeave", asManager.Employees.Single(e => e.Id == colleague.Id).Absences.Single().Type);
    }

    [Fact]
    public async Task Only_a_foreman_can_hold_the_right_and_an_administrator_gives_it()
    {
        var unit = await NewBranchAsync();
        var foreman = await ForemanAsync(unit, canPlan: false);
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var workerUser = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, worker.Id));

        await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(Guid.NewGuid(), UserRole.Admin, null, "admin@example.com");
            await scope.Send(new UpdateUserCommand { Id = foreman.User.Id, Email = foreman.User.Email, Role = UserRole.Foreman, CanPlan = true });
            await scope.Send(new UpdateUserCommand { Id = workerUser.Id, Email = workerUser.Email, Role = UserRole.Worker, CanPlan = true });
        });

        var after = await InScope(async scope => (
            await scope.Db.Users.SingleAsync(u => u.Id == foreman.User.Id),
            await scope.Db.Users.SingleAsync(u => u.Id == workerUser.Id)));

        Assert.True(after.Item1.CanPlan);
        Assert.False(after.Item2.CanPlan);
    }
}
