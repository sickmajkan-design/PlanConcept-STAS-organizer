using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;
using Construction.Application.Features.Employees.Commands.DeleteEmployee;
using Construction.Application.Features.Projects.Commands.CreateProject;
using Construction.Application.Features.Projects.Commands.DeleteProject;
using Construction.Application.Features.Projects.Commands.UpdateProject;
using Construction.Application.Features.Projects.Queries.GetProjectById;
using Construction.Application.Features.Projects.Queries.GetProjects;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Creating, editing and reading a project, and the list behind the picker.
/// </summary>
/// <remarks>
/// Only delete had a test before this, and only incidentally, as setup for
/// the tool-assignment rules. Everything here is either a write that has to
/// survive a round trip, a detail view the panel depends on, or a filter whose
/// failure mode is a wrong list rather than an error.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class ProjectTests : IntegrationTestBase
{
    public ProjectTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static CreateProjectCommand New(
        string name,
        string? client = null,
        ProjectStatus status = ProjectStatus.Planned) => new()
        {
            Name = name,
            Client = client,
            Status = status
        };

    // ---- create ----------------------------------------------------------

    [Fact]
    public async Task A_created_project_reads_back_as_it_was_written()
    {
        var created = await InScope(scope => scope.Send(new CreateProjectCommand
        {
            Name = "  Warehouse in Sesvete  ",
            Description = " Two bays and an office. ",
            Client = " Logistika d.o.o. ",
            Address = " Sesvetska 12 ",
            Latitude = 45.8317,
            Longitude = 16.1122,
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 3, 31),
            Status = ProjectStatus.Active
        }));

        var detail = await InScope(scope => scope.Send(new GetProjectByIdQuery(created.Id)));

        // Trimmed on the way in — a name with a trailing space sorts and
        // searches as a different name from the one on screen.
        Assert.Equal("Warehouse in Sesvete", detail.Name);
        Assert.Equal("Two bays and an office.", detail.Description);
        Assert.Equal("Logistika d.o.o.", detail.Client);
        Assert.Equal("Sesvetska 12", detail.Address);
        Assert.Equal(45.8317, detail.Latitude!.Value, 4);
        Assert.Equal(16.1122, detail.Longitude!.Value, 4);
        Assert.Equal(new DateOnly(2026, 9, 1), detail.StartDate);
        Assert.Equal(new DateOnly(2027, 3, 31), detail.EndDate);
        Assert.Equal(nameof(ProjectStatus.Active), detail.Status);
    }

    [Fact]
    public async Task A_project_can_be_created_with_no_site_coordinates()
    {
        // Most projects are entered before anybody pins them on a map, and an
        // office refurbishment may never be pinned at all.
        var created = await InScope(scope => scope.Send(New("Office refit")));

        var detail = await InScope(scope => scope.Send(new GetProjectByIdQuery(created.Id)));

        Assert.Null(detail.Latitude);
        Assert.Null(detail.Longitude);
    }

    [Fact]
    public async Task Half_a_coordinate_is_refused()
    {
        // A latitude with no longitude is not a location. Stored, it would put
        // the site marker on the prime meridian.
        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
            scope.Send(new CreateProjectCommand { Name = "Half pinned", Latitude = 45.81 })));
    }

    [Fact]
    public async Task A_project_cannot_end_before_it_starts()
    {
        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
            scope.Send(new CreateProjectCommand
            {
                Name = "Backwards",
                StartDate = new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2026, 8, 1)
            })));
    }

    [Fact]
    public async Task Two_projects_may_share_a_name()
    {
        // Deliberate: unlike an employee number, a project name is a label and
        // not an identifier. "Warehouse" happens twice, in two towns, and
        // refusing the second would be wrong.
        var first = await InScope(scope => scope.Send(New("Warehouse")));
        var second = await InScope(scope => scope.Send(New("Warehouse")));

        Assert.NotEqual(first.Id, second.Id);
    }

    // ---- update ----------------------------------------------------------

    [Fact]
    public async Task An_edit_saves_what_was_changed()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        var updated = await InScope(scope => scope.Send(new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Renamed site",
            Client = "New client",
            Status = ProjectStatus.Completed
        }));

        Assert.Equal("Renamed site", updated.Name);
        Assert.Equal("New client", updated.Client);
        Assert.Equal(nameof(ProjectStatus.Completed), updated.Status);
    }

    [Fact]
    public async Task An_edit_can_clear_a_field_that_had_a_value()
    {
        // The command sends the whole record, so an omitted optional field
        // means "cleared". A handler that only wrote non-nulls would make it
        // impossible to remove a client or unpin a site from the panel.
        var project = await InScope(scope => scope.Send(new CreateProjectCommand
        {
            Name = "Pinned",
            Client = "Somebody",
            Latitude = 45.81,
            Longitude = 15.98
        }));

        var updated = await InScope(scope => scope.Send(new UpdateProjectCommand
        {
            Id = project.Id,
            Name = "Pinned"
        }));

        Assert.Null(updated.Client);
        Assert.Null(updated.Latitude);
        Assert.Null(updated.Longitude);
    }

    [Fact]
    public async Task Editing_a_project_that_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
            scope.Send(new UpdateProjectCommand { Id = Guid.NewGuid(), Name = "Ghost" })));
    }

    [Fact]
    public async Task Editing_a_deleted_project_reports_not_found()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(new DeleteProjectCommand(project.Id)));

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
            scope.Send(new UpdateProjectCommand { Id = project.Id, Name = "Revived" })));
    }

    // ---- the detail view -------------------------------------------------

    [Fact]
    public async Task The_detail_view_lists_the_crew()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var employee = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, firstName: "Luka", lastName: "Novak"));

        await InScope(scope =>
            scope.Send(new AssignEmployeeToProjectCommand(employee.Id, project.Id)));

        var detail = await InScope(scope => scope.Send(new GetProjectByIdQuery(project.Id)));

        var member = Assert.Single(detail.Employees, e => e.EmployeeId == employee.Id);

        Assert.Equal("Luka Novak", member.FullName);
        Assert.Equal(employee.EmployeeNumber, member.EmployeeNumber);
    }

    [Fact]
    public async Task A_deleted_employee_leaves_the_crew_list()
    {
        // The posting row survives the employee's soft delete, so without the
        // filter reaching through the join the crew list would keep showing
        // somebody who no longer works here — with their name, on a screen
        // used to decide who is on site.
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope =>
            scope.Send(new AssignEmployeeToProjectCommand(employee.Id, project.Id)));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        var detail = await InScope(scope => scope.Send(new GetProjectByIdQuery(project.Id)));

        Assert.DoesNotContain(detail.Employees, e => e.EmployeeId == employee.Id);
    }

    [Fact]
    public async Task Reading_a_project_that_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetProjectByIdQuery(Guid.NewGuid()))));
    }

    [Fact]
    public async Task Reading_a_deleted_project_reports_not_found()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(new DeleteProjectCommand(project.Id)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetProjectByIdQuery(project.Id))));
    }

    // ---- listing ---------------------------------------------------------

    [Fact]
    public async Task Search_covers_the_name_the_client_and_the_address()
    {
        var project = await InScope(scope => scope.Send(new CreateProjectCommand
        {
            Name = "Findable site",
            Client = "Gradnja Uniquename",
            Address = "Distinctivestreet 4"
        }));

        foreach (var term in new[] { "Findable", "Uniquename", "Distinctivestreet" })
        {
            var page = await InScope(scope =>
                scope.Send(new GetProjectsQuery { Search = term, PageSize = 100 }));

            Assert.Contains(page.Items, p => p.Id == project.Id);
        }
    }

    [Fact]
    public async Task A_wildcard_typed_into_the_project_search_is_taken_literally()
    {
        var literal = await InScope(scope => scope.Send(New("Ninety%Complete")));
        var ordinary = await InScope(scope => scope.Send(New("Nothing special here")));

        var page = await InScope(scope =>
            scope.Send(new GetProjectsQuery { Search = "%", PageSize = 100 }));

        Assert.Contains(page.Items, p => p.Id == literal.Id);
        Assert.DoesNotContain(page.Items, p => p.Id == ordinary.Id);
    }

    [Fact]
    public async Task A_wildcard_in_the_client_filter_is_taken_literally_too()
    {
        // The client filter is a separate code path from the search box, and
        // it used to build its pattern inline without escaping anything.
        var literal = await InScope(scope =>
            scope.Send(New("Site A", client: "Fifty%Percent d.o.o.")));
        var ordinary = await InScope(scope =>
            scope.Send(New("Site B", client: "Ordinary Client d.o.o.")));

        var page = await InScope(scope =>
            scope.Send(new GetProjectsQuery { Client = "%", PageSize = 100 }));

        Assert.Contains(page.Items, p => p.Id == literal.Id);
        Assert.DoesNotContain(page.Items, p => p.Id == ordinary.Id);
    }

    [Fact]
    public async Task The_list_can_be_narrowed_to_a_status()
    {
        // Scoped by a name only this test uses, so the assertion does not
        // depend on how many completed projects the rest of the suite left
        // behind.
        const string name = "Statusproject";

        var active = await InScope(scope => scope.Send(New(name, status: ProjectStatus.Active)));
        var done = await InScope(scope => scope.Send(New(name, status: ProjectStatus.Completed)));

        var page = await InScope(scope => scope.Send(new GetProjectsQuery
        {
            Search = name,
            Status = ProjectStatus.Completed,
            PageSize = 100
        }));

        Assert.Contains(page.Items, p => p.Id == done.Id);
        Assert.DoesNotContain(page.Items, p => p.Id == active.Id);
    }

    [Fact]
    public async Task The_list_can_be_narrowed_to_the_projects_one_person_is_posted_to()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var theirs = await InScope(scope => TestData.SeedProjectAsync(scope));
        var somebody_elses = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope =>
            scope.Send(new AssignEmployeeToProjectCommand(employee.Id, theirs.Id)));

        var page = await InScope(scope => scope.Send(new GetProjectsQuery
        {
            EmployeeId = employee.Id,
            PageSize = 100
        }));

        Assert.Contains(page.Items, p => p.Id == theirs.Id);
        Assert.DoesNotContain(page.Items, p => p.Id == somebody_elses.Id);
    }

    [Fact]
    public async Task A_deleted_project_leaves_the_list()
    {
        var project = await InScope(scope =>
            TestData.SeedProjectAsync(scope, name: "Deletable Uniqueproject"));

        var before = await InScope(scope =>
            scope.Send(new GetProjectsQuery { Search = "Uniqueproject", PageSize = 100 }));
        Assert.Contains(before.Items, p => p.Id == project.Id);

        await InScope(scope => scope.Send(new DeleteProjectCommand(project.Id)));

        var after = await InScope(scope =>
            scope.Send(new GetProjectsQuery { Search = "Uniqueproject", PageSize = 100 }));
        Assert.DoesNotContain(after.Items, p => p.Id == project.Id);
    }

    [Fact]
    public async Task Paging_a_list_of_namesakes_neither_repeats_nor_skips_anything()
    {
        const string name = "Pagedproject";

        for (var i = 0; i < 12; i++)
        {
            await InScope(scope => scope.Send(New(name)));
        }

        var pages = new List<Guid>();

        for (var page = 1; page <= 3; page++)
        {
            var result = await InScope(scope => scope.Send(new GetProjectsQuery
            {
                Search = name,
                PageNumber = page,
                PageSize = 5
            }));

            pages.AddRange(result.Items.Select(p => p.Id));
        }

        Assert.Equal(12, pages.Count);
        Assert.Equal(12, pages.Distinct().Count());
    }
}
