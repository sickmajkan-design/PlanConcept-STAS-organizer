using System.Net;
using System.Net.Http.Json;
using Construction.Application.Features.Planning;
using Construction.Application.Features.Postings;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class PlanningTests : IntegrationTestBase
{
    public PlanningTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly D(int day) => new(2031, 3, day);

    private Task<List<EmployeeProject>> PostingsAsync(Guid employeeId) =>
        InScope(scope => scope.Db.EmployeeProjects
            .Where(ep => ep.EmployeeId == employeeId)
            .OrderBy(ep => ep.StartDate)
            .ToListAsync());

    private Task Assign(Guid employeeId, Guid? projectId, int from, int to, bool onlyFree = false) =>
        InScope(scope => scope.Send(new SetEmployeeScheduleCommand
        {
            EmployeeId = employeeId,
            ProjectId = projectId,
            From = D(from),
            To = D(to),
            OnlyFreeDays = onlyFree,
        }));

    [Fact]
    public async Task Putting_someone_on_a_site_creates_a_posting_for_exactly_those_days()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));

        await Assign(employee.Id, site.Id, 3, 14);

        var posting = Assert.Single(await PostingsAsync(employee.Id));
        Assert.Equal(site.Id, posting.ProjectId);
        Assert.Equal(D(3), posting.StartDate);
        Assert.Equal(D(14), posting.EndDate);
    }

    [Fact]
    public async Task Moving_someone_for_a_week_splits_their_longer_stay_and_sending_it_twice_changes_nothing()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var first = await InScope(scope => TestData.SeedProjectAsync(scope));
        var second = await InScope(scope => TestData.SeedProjectAsync(scope));

        await Assign(employee.Id, first.Id, 1, 28);
        await Assign(employee.Id, second.Id, 10, 14);
        await Assign(employee.Id, second.Id, 10, 14);

        var postings = await PostingsAsync(employee.Id);
        Assert.Equal(3, postings.Count);
        Assert.Equal((first.Id, D(1), (DateOnly?)D(9)), (postings[0].ProjectId, postings[0].StartDate, postings[0].EndDate));
        Assert.Equal((second.Id, D(10), (DateOnly?)D(14)), (postings[1].ProjectId, postings[1].StartDate, postings[1].EndDate));
        Assert.Equal((first.Id, D(15), (DateOnly?)D(28)), (postings[2].ProjectId, postings[2].StartDate, postings[2].EndDate));
    }

    [Fact]
    public async Task Freeing_someone_for_a_range_leaves_their_other_days()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));

        await Assign(employee.Id, site.Id, 1, 20);
        await Assign(employee.Id, null, 8, 12);

        var postings = await PostingsAsync(employee.Id);
        Assert.Equal(2, postings.Count);
        Assert.Equal(D(7), postings[0].EndDate);
        Assert.Equal(D(13), postings[1].StartDate);
    }

    [Fact]
    public async Task Only_free_days_are_filled_when_asked_so_a_stand_in_keeps_their_own_work()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var own = await InScope(scope => TestData.SeedProjectAsync(scope));
        var cover = await InScope(scope => TestData.SeedProjectAsync(scope));

        await Assign(employee.Id, own.Id, 5, 8);
        await Assign(employee.Id, cover.Id, 3, 12, onlyFree: true);

        var postings = await PostingsAsync(employee.Id);
        Assert.Contains(postings, p => p.ProjectId == own.Id && p.StartDate == D(5) && p.EndDate == D(8));
        Assert.Contains(postings, p => p.ProjectId == cover.Id && p.StartDate == D(3) && p.EndDate == D(4));
        Assert.Contains(postings, p => p.ProjectId == cover.Id && p.StartDate == D(9) && p.EndDate == D(12));
    }

    [Fact]
    public async Task Nobody_can_be_posted_to_a_finished_project()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var done = await InScope(scope => TestData.SeedProjectAsync(scope, status: ProjectStatus.Completed));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => Assign(employee.Id, done.Id, 1, 5));
    }

    [Fact]
    public async Task Swapping_two_people_trades_their_sites_for_the_range_only()
    {
        var a = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var b = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var siteA = await InScope(scope => TestData.SeedProjectAsync(scope));
        var siteB = await InScope(scope => TestData.SeedProjectAsync(scope));

        await Assign(a.Id, siteA.Id, 1, 20);
        await Assign(b.Id, siteB.Id, 1, 20);

        await InScope(scope => scope.Send(new SwapEmployeeSchedulesCommand
        {
            EmployeeAId = a.Id,
            EmployeeBId = b.Id,
            From = D(8),
            To = D(12),
        }));

        var forA = await PostingsAsync(a.Id);
        Assert.Contains(forA, p => p.ProjectId == siteB.Id && p.StartDate == D(8) && p.EndDate == D(12));
        Assert.Contains(forA, p => p.ProjectId == siteA.Id && p.StartDate == D(1) && p.EndDate == D(7));
        Assert.Contains(forA, p => p.ProjectId == siteA.Id && p.StartDate == D(13) && p.EndDate == D(20));

        var forB = await PostingsAsync(b.Id);
        Assert.Contains(forB, p => p.ProjectId == siteA.Id && p.StartDate == D(8) && p.EndDate == D(12));
        Assert.Equal(3, forB.Count);
    }

    [Fact]
    public async Task Needs_are_replaced_as_a_set_and_the_same_position_in_two_spellings_is_one_need()
    {
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand
        {
            ProjectId = site.Id,
            Needs = [new("Zidar", 2), new(" zidar ", 1), new("Električar", 1)],
        }));
        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand
        {
            ProjectId = site.Id,
            Needs = [new("Zidar", 4), new("Vozač", 0)],
        }));

        var needs = await InScope(scope => scope.Db.ProjectStaffingNeeds.Where(n => n.ProjectId == site.Id).ToListAsync());
        var need = Assert.Single(needs);
        Assert.Equal("Zidar", need.Position);
        Assert.Equal(4, need.Count);
    }

    [Fact]
    public async Task The_planning_view_carries_postings_needs_and_positions()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope, name: $"Plan {Guid.NewGuid():N}"));

        await Assign(employee.Id, site.Id, 2, 25);
        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand
        {
            ProjectId = site.Id,
            Needs = [new("Site Manager", 2)],
        }));

        var plan = await InScope(scope => scope.Send(new GetPlanningQuery { From = D(1), To = D(30) }));

        var row = plan.Employees.Single(e => e.Id == employee.Id);
        Assert.Equal(site.Id, Assert.Single(row.Postings).ProjectId);
        var project = plan.Projects.Single(p => p.Id == site.Id);
        Assert.Equal(2, Assert.Single(project.Needs).Count);
        Assert.Contains("Site Manager", plan.Positions);
    }

    [Fact]
    public async Task A_window_longer_than_the_limit_is_refused()
    {
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(() =>
            InScope(scope => scope.Send(new GetPlanningQuery { From = D(1), To = D(1).AddDays(PlanningLimits.MaxDays + 5) })));
    }
}

[Collection(DatabaseCollection.Name)]
public class PostingAcknowledgementTests : IntegrationTestBase
{
    public PostingAcknowledgementTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly D(int day) => new(2031, 4, day);

    private async Task<(Employee Worker, Project Site, Guid PostingId)> PostedAsync()
    {
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = worker.Id, ProjectId = site.Id, From = D(1), To = D(20) }));

        var id = await InScope(scope => scope.Db.EmployeeProjects.Where(ep => ep.EmployeeId == worker.Id).Select(ep => ep.Id).SingleAsync());
        return (worker, site, id);
    }

    private Task Acknowledge(Guid postingId, Guid asEmployee) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(Guid.NewGuid(), UserRole.Worker, asEmployee, "worker@example.com");
            return scope.Send(new AcknowledgePostingCommand(postingId));
        });

    private Task<DateTime?> AcknowledgedAt(Guid postingId) =>
        InScope(scope => scope.Db.EmployeeProjects.Where(ep => ep.Id == postingId).Select(ep => ep.AcknowledgedAt).SingleAsync());

    [Fact]
    public async Task A_new_posting_waits_for_the_worker_and_they_can_confirm_it_once()
    {
        var (worker, _, id) = await PostedAsync();

        Assert.Null(await AcknowledgedAt(id));

        await Acknowledge(id, worker.Id);
        var first = await AcknowledgedAt(id);
        Assert.NotNull(first);

        await Acknowledge(id, worker.Id);
        Assert.Equal(first, await AcknowledgedAt(id));
    }

    [Fact]
    public async Task Nobody_can_confirm_somebody_elses_posting()
    {
        var (_, _, id) = await PostedAsync();
        var other = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.NotFoundException>(() => Acknowledge(id, other.Id));
        Assert.Null(await AcknowledgedAt(id));
    }

    [Fact]
    public async Task Changing_the_days_of_a_confirmed_posting_asks_for_confirmation_again_but_other_changes_do_not()
    {
        var (worker, site, id) = await PostedAsync();
        await Acknowledge(id, worker.Id);

        // The same site over days it already covers: nothing changed.
        await InScope(scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = worker.Id, ProjectId = site.Id, From = D(5), To = D(10) }));
        Assert.NotNull(await AcknowledgedAt(id));

        // Cutting the posting short is a change the worker has not seen.
        await InScope(scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = worker.Id, ProjectId = null, From = D(15), To = D(20) }));
        Assert.Null(await AcknowledgedAt(id));
    }

    [Fact]
    public async Task The_planning_view_and_the_phones_schedule_both_say_whether_it_is_confirmed()
    {
        var (worker, _, id) = await PostedAsync();
        await Acknowledge(id, worker.Id);

        var plan = await InScope(scope => scope.Send(new GetPlanningQuery { From = D(1), To = D(30) }));

        Assert.NotNull(plan.Employees.Single(e => e.Id == worker.Id).Postings.Single().AcknowledgedAt);
    }
}

[Collection(ApiCollection.Name)]
public class PlanningHttpTests
{
    private readonly ApiFixture _api;

    public PlanningHttpTests(ApiFixture api)
    {
        _api = api;
    }

    [Theory]
    [InlineData(UserRole.Foreman, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Worker, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.ProjectManager, HttpStatusCode.OK)]
    [InlineData(UserRole.Admin, HttpStatusCode.OK)]
    public async Task Only_a_project_manager_and_above_may_read_the_plan(UserRole role, HttpStatusCode expected)
    {
        using var client = _api.ClientAs(role);

        var response = await client.GetAsync("/api/v1/planning?from=2031-03-01&to=2031-03-31");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task A_foreman_cannot_move_people()
    {
        using var client = _api.ClientAs(UserRole.Foreman);

        var response = await client.PostAsJsonAsync("/api/v1/planning/assign", new
        {
            employeeId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            from = "2031-03-01",
            to = "2031-03-05",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
