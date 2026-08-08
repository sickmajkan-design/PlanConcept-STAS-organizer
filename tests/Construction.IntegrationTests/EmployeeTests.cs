using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;
using Construction.Application.Features.Employees.Commands.CreateEmployee;
using Construction.Application.Features.Employees.Commands.DeleteEmployee;
using Construction.Application.Features.Employees.Commands.UpdateEmployee;
using Construction.Application.Features.Employees.Queries.GetEmployeeById;
using Construction.Application.Features.Employees.Queries.GetEmployees;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Editing an employee, reading one back, and the search behind the list.
/// </summary>
/// <remarks>
/// <para>
/// Create and delete were already covered by <c>SoftDeleteTests</c>, which is
/// where the filtered-index behaviour lives. What had no test was update — the
/// command that can take a number away from somebody — the detail query, and
/// the search.
/// </para>
/// <para>
/// The search tests are the reason this file runs against PostgreSQL. The
/// wildcard escaping is a `LIKE` pattern built in C# and interpreted by the
/// database; asserting it anywhere else asserts the string, not the search.
/// </para>
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class EmployeeTests : IntegrationTestBase
{
    public EmployeeTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static UpdateEmployeeCommand Edit(
        Guid id,
        string number,
        string firstName = "Ivan",
        string lastName = "Horvat",
        string? email = null,
        EmployeeStatus status = EmployeeStatus.Active) => new()
        {
            Id = id,
            EmployeeNumber = number,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Position = "Site Manager",
            EmploymentDate = new DateOnly(2020, 3, 1),
            Status = status
        };

    // ---- update ----------------------------------------------------------

    [Fact]
    public async Task An_edit_saves_what_was_changed()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var updated = await InScope(scope => scope.Send(
            Edit(employee.Id, employee.EmployeeNumber, "Petra", "Kovac",
                status: EmployeeStatus.OnLeave)));

        Assert.Equal("Petra", updated.FirstName);
        Assert.Equal("Kovac", updated.LastName);
        Assert.Equal(nameof(EmployeeStatus.OnLeave), updated.Status);

        var reloaded = await InScope(scope =>
            scope.Send(new GetEmployeeByIdQuery(employee.Id)));

        Assert.Equal("Petra", reloaded.FirstName);
    }

    [Fact]
    public async Task An_edit_that_changes_nothing_does_not_collide_with_the_employee_s_own_number()
    {
        // The uniqueness check has to exclude the row being edited. Without
        // that, saving a form without touching the number — which is most
        // saves — reports the number as taken by the person who holds it.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var updated = await InScope(scope => scope.Send(
            Edit(employee.Id, employee.EmployeeNumber, lastName: "Horvat-Novak")));

        Assert.Equal(employee.EmployeeNumber, updated.EmployeeNumber);
        Assert.Equal("Horvat-Novak", updated.LastName);
    }

    [Fact]
    public async Task An_edit_cannot_take_a_number_that_somebody_else_holds()
    {
        var taken = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, employeeNumber: "EMP-HELD-01"));
        var other = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            InScope(scope => scope.Send(Edit(other.Id, taken.EmployeeNumber))));

        Assert.Contains("EMP-HELD-01", error.Message);
    }

    [Fact]
    public async Task An_edit_can_take_a_number_whose_previous_holder_was_deleted()
    {
        // Consistent with the filtered unique index and with create. If the
        // check ignored the filter, a number would be permanently burned by
        // the first person to hold it.
        const string number = "EMP-FREED-01";

        var original = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, employeeNumber: number));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(original.Id)));

        var successor = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var updated = await InScope(scope => scope.Send(Edit(successor.Id, number)));

        Assert.Equal(number, updated.EmployeeNumber);
    }

    [Fact]
    public async Task An_edit_normalises_what_was_typed()
    {
        // Email is lowercased because it is matched against elsewhere, and
        // everything is trimmed because a trailing space in an employee number
        // makes a lookup fail in a way nobody can see on screen.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var updated = await InScope(scope => scope.Send(new UpdateEmployeeCommand
        {
            Id = employee.Id,
            EmployeeNumber = "  EMP-SPACED-01  ",
            FirstName = "  Petra ",
            LastName = " Kovac  ",
            Email = "  Petra.Kovac@EXAMPLE.COM ",
            Position = " Foreman ",
            EmploymentDate = new DateOnly(2020, 3, 1),
            Status = EmployeeStatus.Active
        }));

        Assert.Equal("EMP-SPACED-01", updated.EmployeeNumber);
        Assert.Equal("Petra", updated.FirstName);
        Assert.Equal("Kovac", updated.LastName);
        Assert.Equal("petra.kovac@example.com", updated.Email);
        Assert.Equal("Foreman", updated.Position);
    }

    [Fact]
    public async Task Editing_somebody_who_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(Edit(Guid.NewGuid(), "EMP-GHOST-01"))));
    }

    [Fact]
    public async Task Editing_a_deleted_employee_reports_not_found()
    {
        // Not "succeeds silently against a hidden row". The soft-delete filter
        // is what makes this true, and an edit that resurrected somebody by
        // writing to them would be worse than an error.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(Edit(employee.Id, "EMP-REVIVE-01"))));
    }

    // ---- the detail view -------------------------------------------------

    [Fact]
    public async Task The_detail_view_lists_the_projects_somebody_is_posted_to()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope =>
            TestData.SeedProjectAsync(scope, name: "Bridge over the Sava"));

        await InScope(scope =>
            scope.Send(new AssignEmployeeToProjectCommand(employee.Id, project.Id)));

        var detail = await InScope(scope =>
            scope.Send(new GetEmployeeByIdQuery(employee.Id)));

        var posting = Assert.Single(detail.Projects, p => p.ProjectId == project.Id);
        Assert.Equal("Bridge over the Sava", posting.ProjectName);
    }

    [Fact]
    public async Task The_detail_view_says_whether_there_is_an_account_without_saying_more()
    {
        // The panel uses this to decide whether to offer "create account".
        // It is a flag rather than the user object on purpose — a detail
        // endpoint for an employee has no business carrying a password hash.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var before = await InScope(scope => scope.Send(new GetEmployeeByIdQuery(employee.Id)));
        Assert.False(before.HasUserAccount);

        await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var after = await InScope(scope => scope.Send(new GetEmployeeByIdQuery(employee.Id)));
        Assert.True(after.HasUserAccount);
    }

    [Fact]
    public async Task Reading_an_employee_who_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetEmployeeByIdQuery(Guid.NewGuid()))));
    }

    [Fact]
    public async Task Reading_a_deleted_employee_reports_not_found()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetEmployeeByIdQuery(employee.Id))));
    }

    // ---- search ----------------------------------------------------------

    private Task<List<Guid>> SearchAsync(string term) =>
        InScope(async scope =>
        {
            var page = await scope.Send(new GetEmployeesQuery { Search = term, PageSize = 100 });
            return page.Items.Select(i => i.Id).ToList();
        });

    [Fact]
    public async Task Search_matches_a_name_across_the_gap_between_first_and_last()
    {
        // The columns are separate; the search concatenates them. Typing a
        // full name is the most obvious thing to do and would otherwise find
        // nobody.
        var employee = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, firstName: "Marija", lastName: "Babic"));

        Assert.Contains(employee.Id, await SearchAsync("Marija Babic"));
    }

    [Fact]
    public async Task Search_ignores_case()
    {
        var employee = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: "Vukovic"));

        Assert.Contains(employee.Id, await SearchAsync("VUKOVIC"));
    }

    [Fact]
    public async Task A_percent_sign_in_the_search_box_means_a_percent_sign()
    {
        // Unescaped, this is a wildcard: the term matches every employee in
        // the company, and the operator sees a full list where they expected
        // one row. It is also the cheap half of a denial of service, since a
        // term made of wildcards turns a scan into a much more expensive one.
        var literal = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: "Ten%Percent"));
        var ordinary = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: "Ordinary"));

        var results = await SearchAsync("%");

        Assert.Contains(literal.Id, results);
        Assert.DoesNotContain(ordinary.Id, results);
    }

    [Fact]
    public async Task An_underscore_in_the_search_box_means_an_underscore()
    {
        // The quieter wildcard: `_` matches any single character, so a term
        // like "a_c" silently matches more than it says.
        var literal = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: "Under_Score"));
        var decoy = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: "UnderXScore"));

        var results = await SearchAsync("Under_Score");

        Assert.Contains(literal.Id, results);
        Assert.DoesNotContain(decoy.Id, results);
    }

    [Fact]
    public async Task A_backslash_in_the_search_box_means_a_backslash()
    {
        // The escape character itself. Escaping it second would double the
        // escapes added for % and _ and turn the pattern into nonsense.
        var literal = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: @"Back\Slash"));

        Assert.Contains(literal.Id, await SearchAsync(@"Back\Slash"));
    }

    [Fact]
    public async Task Search_covers_the_employee_number_and_the_position_too()
    {
        // Both terms have to be unique to this test. The seed helper's default
        // position is shared with most of the suite, and searching for it
        // returns more than a page of other tests' employees — which fails
        // here and nowhere near the cause.
        var created = await InScope(scope => scope.Send(new CreateEmployeeCommand
        {
            EmployeeNumber = "EMP-FINDME-01",
            FirstName = "Iva",
            LastName = "Findable",
            Position = "Scaffoldinspector",
            EmploymentDate = new DateOnly(2021, 1, 4),
            Status = EmployeeStatus.Active
        }));

        Assert.Contains(created.Id, await SearchAsync("FINDME"));
        Assert.Contains(created.Id, await SearchAsync("Scaffoldinspector"));
    }

    // ---- listing ---------------------------------------------------------

    [Fact]
    public async Task The_list_can_be_narrowed_to_one_project()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var posted = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var elsewhere = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope =>
            scope.Send(new AssignEmployeeToProjectCommand(posted.Id, project.Id)));

        var crew = await InScope(scope => scope.Send(new GetEmployeesQuery
        {
            ProjectId = project.Id,
            PageSize = 100
        }));

        Assert.Contains(crew.Items, e => e.Id == posted.Id);
        Assert.DoesNotContain(crew.Items, e => e.Id == elsewhere.Id);
    }

    [Fact]
    public async Task The_list_can_be_narrowed_to_a_status()
    {
        // Scoped by a surname only this test uses. Filtering on status alone
        // would return every terminated employee the suite has ever seeded,
        // and the two this test cares about could fall past the page.
        const string surname = "Statustest";

        var active = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: surname));
        var left = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, lastName: surname, status: EmployeeStatus.Terminated));

        var page = await InScope(scope => scope.Send(new GetEmployeesQuery
        {
            Search = surname,
            Status = EmployeeStatus.Terminated,
            PageSize = 100
        }));

        Assert.Contains(page.Items, e => e.Id == left.Id);
        Assert.DoesNotContain(page.Items, e => e.Id == active.Id);
    }

    [Fact]
    public async Task Paging_a_list_of_namesakes_neither_repeats_nor_skips_anybody()
    {
        // The reason for the id tiebreaker. Twelve people sharing a surname
        // sort equally, and without a stable last key PostgreSQL is free to
        // order them differently between the two queries that fetch page one
        // and page two — so somebody appears twice and somebody never appears.
        const string surname = "Pagingtest";

        var seeded = new List<Guid>();

        for (var i = 0; i < 12; i++)
        {
            var employee = await InScope(scope =>
                TestData.SeedEmployeeAsync(scope, firstName: "Same", lastName: surname));
            seeded.Add(employee.Id);
        }

        var first = await InScope(scope => scope.Send(new GetEmployeesQuery
        {
            Search = surname,
            PageNumber = 1,
            PageSize = 5
        }));

        var second = await InScope(scope => scope.Send(new GetEmployeesQuery
        {
            Search = surname,
            PageNumber = 2,
            PageSize = 5
        }));

        var third = await InScope(scope => scope.Send(new GetEmployeesQuery
        {
            Search = surname,
            PageNumber = 3,
            PageSize = 5
        }));

        Assert.Equal(12, first.TotalCount);

        var seen = first.Items.Concat(second.Items).Concat(third.Items)
            .Select(i => i.Id)
            .ToList();

        Assert.Equal(12, seen.Count);
        Assert.Equal(12, seen.Distinct().Count());
        Assert.All(seeded, id => Assert.Contains(id, seen));
    }

    [Fact]
    public async Task Sorting_by_a_column_actually_sorts_by_it()
    {
        const string surname = "Sorttest";

        foreach (var number in new[] { "EMP-SORT-C", "EMP-SORT-A", "EMP-SORT-B" })
        {
            await InScope(scope => TestData.SeedEmployeeAsync(
                scope, employeeNumber: number, lastName: surname));
        }

        var ascending = await InScope(scope => scope.Send(new GetEmployeesQuery
        {
            Search = surname,
            SortBy = "employeeNumber",
            PageSize = 100
        }));

        var descending = await InScope(scope => scope.Send(new GetEmployeesQuery
        {
            Search = surname,
            SortBy = "employeeNumber",
            SortDescending = true,
            PageSize = 100
        }));

        Assert.Equal(
            ["EMP-SORT-A", "EMP-SORT-B", "EMP-SORT-C"],
            ascending.Items.Select(i => i.EmployeeNumber));

        Assert.Equal(
            ["EMP-SORT-C", "EMP-SORT-B", "EMP-SORT-A"],
            descending.Items.Select(i => i.EmployeeNumber));
    }

    [Fact]
    public async Task A_created_employee_is_findable_by_everything_they_were_created_with()
    {
        // Create is covered elsewhere for the conflict rules; this is about
        // the write and the read agreeing, which is where a mapping mistake
        // shows up.
        var created = await InScope(scope => scope.Send(new CreateEmployeeCommand
        {
            EmployeeNumber = "EMP-ROUNDTRIP-01",
            FirstName = "Ana",
            LastName = "Maric",
            Email = "Ana.Maric@Example.com",
            Phone = "+385 91 000 0000",
            Position = "Electrician",
            DateOfBirth = new DateOnly(1990, 4, 2),
            EmploymentDate = new DateOnly(2021, 6, 1),
            Status = EmployeeStatus.Active
        }));

        var detail = await InScope(scope => scope.Send(new GetEmployeeByIdQuery(created.Id)));

        Assert.Equal("EMP-ROUNDTRIP-01", detail.EmployeeNumber);
        Assert.Equal("Ana", detail.FirstName);
        Assert.Equal("Maric", detail.LastName);
        Assert.Equal("ana.maric@example.com", detail.Email);
        Assert.Equal("Electrician", detail.Position);
        Assert.Equal(new DateOnly(2021, 6, 1), detail.EmploymentDate);
    }
}
