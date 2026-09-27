using Construction.Application.Common.Exceptions;
using Construction.Application.Features.CustomerCompanies;
using Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;
using Construction.Application.Features.Employees.Commands.SetEmployeeProjectCompany;
using Construction.Application.Features.Employees.Queries.GetEmployeeById;
using Construction.Application.Features.Projects.Queries.GetProjectById;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// B13: which of a client's companies an employee's posting is worked for — so a client with
/// several legal entities can have its invoices, and eventually its payroll, split correctly
/// between them, without anyone re-deriving that split by hand every month.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class EmployeeProjectCompanyTests : IntegrationTestBase
{
    public EmployeeProjectCompanyTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private sealed record Setup(User SuperAdmin, Employee Employee, Project Project, CustomerCompanyDto Company);

    /// <summary>An employee, and a project whose client has one company of its own.</summary>
    private async Task<Setup> SetupAsync()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var customer = await InScope(scope => TestData.SeedCustomerAsync(scope));

        var project = await InScope(async scope =>
        {
            var p = await TestData.SeedProjectAsync(scope);
            p.CustomerId = customer.Id;
            await scope.Db.SaveChangesAsync();
            return p;
        });

        var company = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new CreateCustomerCompanyCommand
            {
                CustomerId = customer.Id,
                Name = $"Firma {Unique()}",
            });
        });

        return new Setup(superAdmin, employee, project, company);
    }

    // ---- assigning with a company --------------------------------------------

    [Fact]
    public async Task A_posting_can_name_which_company_it_is_worked_for()
    {
        var setup = await SetupAsync();

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id)
            {
                CustomerCompanyId = setup.Company.Id,
            });
        });

        var detail = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetEmployeeByIdQuery(setup.Employee.Id));
        });

        var posting = Assert.Single(detail.Projects, p => p.ProjectId == setup.Project.Id);
        Assert.Equal(setup.Company.Id, posting.CustomerCompanyId);
        Assert.Equal(setup.Company.Name, posting.CustomerCompanyName);
    }

    [Fact]
    public async Task The_company_also_shows_on_the_projects_own_crew_list()
    {
        var setup = await SetupAsync();

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id)
            {
                CustomerCompanyId = setup.Company.Id,
            });
        });

        var project = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetProjectByIdQuery(setup.Project.Id));
        });

        var member = Assert.Single(project.Employees, e => e.EmployeeId == setup.Employee.Id);
        Assert.Equal(setup.Company.Name, member.CustomerCompanyName);
    }

    [Fact]
    public async Task Without_a_company_the_posting_is_simply_for_the_client_itself()
    {
        var setup = await SetupAsync();

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id));
        });

        var detail = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetEmployeeByIdQuery(setup.Employee.Id));
        });

        var posting = Assert.Single(detail.Projects, p => p.ProjectId == setup.Project.Id);
        Assert.Null(posting.CustomerCompanyId);
        Assert.Null(posting.CustomerCompanyName);
    }

    [Fact]
    public async Task A_company_from_another_client_is_refused()
    {
        var setup = await SetupAsync();
        var otherCustomer = await InScope(scope => TestData.SeedCustomerAsync(scope));

        var otherCompany = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new CreateCustomerCompanyCommand
            {
                CustomerId = otherCustomer.Id,
                Name = $"Tudja firma {Unique()}",
            });
        });

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id)
            {
                CustomerCompanyId = otherCompany.Id,
            });
        }));
    }

    [Fact]
    public async Task A_project_manager_cannot_name_a_company_when_posting_someone()
    {
        var setup = await SetupAsync();
        var manager = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, manager);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id)
            {
                CustomerCompanyId = setup.Company.Id,
            });
        }));
    }

    [Fact]
    public async Task A_project_manager_may_still_post_someone_without_naming_a_company()
    {
        var setup = await SetupAsync();
        var manager = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        await InScope(scope =>
        {
            ActAs(scope, manager);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id));
        });

        var detail = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetEmployeeByIdQuery(setup.Employee.Id));
        });

        Assert.Single(detail.Projects, p => p.ProjectId == setup.Project.Id);
    }

    // ---- changing it later -----------------------------------------------

    [Fact]
    public async Task The_company_on_an_existing_posting_can_be_changed()
    {
        var setup = await SetupAsync();

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id));
        });

        var secondCompany = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new CreateCustomerCompanyCommand
            {
                CustomerId = setup.Project.CustomerId!.Value,
                Name = $"Druga firma {Unique()}",
            });
        });

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new SetEmployeeProjectCompanyCommand(
                setup.Employee.Id, setup.Project.Id, secondCompany.Id));
        });

        var detail = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetEmployeeByIdQuery(setup.Employee.Id));
        });

        var posting = Assert.Single(detail.Projects, p => p.ProjectId == setup.Project.Id);
        Assert.Equal(secondCompany.Id, posting.CustomerCompanyId);
    }

    [Fact]
    public async Task Clearing_the_company_sends_the_posting_back_to_the_client_itself()
    {
        var setup = await SetupAsync();

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id)
            {
                CustomerCompanyId = setup.Company.Id,
            });
        });

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new SetEmployeeProjectCompanyCommand(setup.Employee.Id, setup.Project.Id, null));
        });

        var detail = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetEmployeeByIdQuery(setup.Employee.Id));
        });

        var posting = Assert.Single(detail.Projects, p => p.ProjectId == setup.Project.Id);
        Assert.Null(posting.CustomerCompanyId);
    }

    [Fact]
    public async Task Only_management_may_change_the_company_on_an_existing_posting()
    {
        var setup = await SetupAsync();
        var manager = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id));
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, manager);
            return scope.Send(new SetEmployeeProjectCompanyCommand(
                setup.Employee.Id, setup.Project.Id, setup.Company.Id));
        }));
    }

    [Fact]
    public async Task Setting_a_company_on_nobodys_posting_is_a_404()
    {
        var setup = await SetupAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new SetEmployeeProjectCompanyCommand(
                setup.Employee.Id, setup.Project.Id, setup.Company.Id));
        }));
    }

    [Fact]
    public async Task Re_posting_someone_already_on_the_site_does_not_disturb_their_company()
    {
        // AssignEmployeeToProjectCommand's "extend the existing posting" path is not the same
        // action as SetEmployeeProjectCompanyCommand, and must not silently clear a company a
        // second, company-less assignment call did not mean to touch.
        var setup = await SetupAsync();

        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id)
            {
                CustomerCompanyId = setup.Company.Id,
            });
        });

        // A plain re-assignment, such as extending the posting's end date, with no company named.
        await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new AssignEmployeeToProjectCommand(setup.Employee.Id, setup.Project.Id)
            {
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            });
        });

        var detail = await InScope(scope =>
        {
            ActAs(scope, setup.SuperAdmin);
            return scope.Send(new GetEmployeeByIdQuery(setup.Employee.Id));
        });

        var posting = Assert.Single(detail.Projects, p => p.ProjectId == setup.Project.Id);
        Assert.Equal(setup.Company.Id, posting.CustomerCompanyId);
    }
}
