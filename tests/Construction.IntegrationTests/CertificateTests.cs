using System.Net;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attention;
using Construction.Application.Features.Certificates;
using Construction.Application.Features.Planning;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class CertificateTests : IntegrationTestBase
{
    public CertificateTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private Task<CertificateDto> Save(Guid employeeId, string name, DateOnly? until = null, Guid? id = null) =>
        InScope(scope => scope.Send(new SaveEmployeeCertificateCommand { EmployeeId = employeeId, Id = id, Name = name, ValidUntil = until }));

    [Fact]
    public async Task A_certificate_can_be_added_changed_and_removed()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var added = await Save(employee.Id, "  Work at height ", new DateOnly(2031, 6, 30));
        Assert.Equal("Work at height", added.Name);

        await Save(employee.Id, "Work at height", new DateOnly(2032, 6, 30), added.Id);
        var list = await InScope(scope => scope.Send(new GetEmployeeCertificatesQuery(employee.Id)));
        var one = Assert.Single(list);
        Assert.Equal(new DateOnly(2032, 6, 30), one.ValidUntil);

        await InScope(scope => scope.Send(new DeleteEmployeeCertificateCommand(employee.Id, added.Id)));
        Assert.Empty(await InScope(scope => scope.Send(new GetEmployeeCertificatesQuery(employee.Id))));
    }

    [Fact]
    public async Task The_same_certificate_in_another_spelling_is_refused_for_one_worker()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await Save(employee.Id, "Forklift licence");

        await Assert.ThrowsAsync<ConflictException>(() => Save(employee.Id, " forklift LICENCE"));
    }

    [Fact]
    public async Task Somebody_elses_certificate_cannot_be_changed_or_removed_through_a_worker()
    {
        var owner = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var other = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var certificate = await Save(owner.Id, "Welding");

        await Assert.ThrowsAsync<NotFoundException>(() => Save(other.Id, "Welding", null, certificate.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope => scope.Send(new DeleteEmployeeCertificateCommand(other.Id, certificate.Id))));
    }

    [Fact]
    public async Task What_a_project_requires_and_what_a_worker_holds_both_reach_the_planning_view()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        await Save(employee.Id, "Work at height", new DateOnly(2031, 6, 30));
        await InScope(scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = employee.Id, ProjectId = site.Id, From = new DateOnly(2031, 6, 1), To = new DateOnly(2031, 6, 20) }));

        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand { ProjectId = site.Id, Needs = [], RequiredCertificates = ["Work at height", " work AT height ", "Welding"] }));

        var plan = await InScope(scope => scope.Send(new GetPlanningQuery { From = new DateOnly(2031, 6, 1), To = new DateOnly(2031, 6, 30) }));

        Assert.Equal(["Welding", "Work at height"], plan.Projects.Single(p => p.Id == site.Id).RequiredCertificates);
        var held = Assert.Single(plan.Employees.Single(e => e.Id == employee.Id).Certificates);
        Assert.Equal(new DateOnly(2031, 6, 30), held.ValidUntil);
        Assert.Contains("Welding", plan.CertificateNames);
    }

    [Fact]
    public async Task Saving_needs_without_a_list_of_certificates_leaves_the_requirements_alone()
    {
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand { ProjectId = site.Id, Needs = [], RequiredCertificates = ["Welding"] }));

        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand { ProjectId = site.Id, Needs = [new("Zidar", 1)] }));

        var count = await InScope(scope => scope.Db.ProjectCertificateRequirements.CountAsync(r => r.ProjectId == site.Id));
        Assert.Equal(1, count);

        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand { ProjectId = site.Id, Needs = [], RequiredCertificates = [] }));
        Assert.Equal(0, await InScope(scope => scope.Db.ProjectCertificateRequirements.CountAsync(r => r.ProjectId == site.Id)));
    }

    [Fact]
    public async Task A_certificate_about_to_run_out_is_told_to_an_administrator()
    {
        var AsAdmin = () => InScope(scope =>
        {
            scope.CurrentUser.SignInAs(Guid.NewGuid(), UserRole.Admin, null, "attention@example.com");
            return scope.Send(new GetAttentionQuery());
        });
        var count = (AttentionDto dto) => dto.Groups.SingleOrDefault(g => g.Key == "certificatesExpiring")?.Count ?? 0;

        var before = count(await AsAdmin());
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await Save(employee.Id, "Forklift", today.AddDays(10));
        await Save(employee.Id, "Welding", today.AddDays(400));
        await Save(employee.Id, "First aid", null);

        Assert.Equal(before + 1, count(await AsAdmin()));
    }
}

[Collection(ApiCollection.Name)]
public class CertificateHttpTests
{
    private readonly ApiFixture _api;

    public CertificateHttpTests(ApiFixture api)
    {
        _api = api;
    }

    [Theory]
    [InlineData(UserRole.Foreman, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Worker, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.ProjectManager, HttpStatusCode.NotFound)]
    public async Task Only_a_project_manager_and_above_may_read_them(UserRole role, HttpStatusCode expected)
    {
        using var client = _api.ClientAs(role);

        var response = await client.GetAsync($"/api/v1/employees/{Guid.NewGuid()}/certificates");

        Assert.Equal(expected, response.StatusCode);
    }
}
