using System.Net;
using Construction.Application.Features.DataQuality;
using Construction.Application.Features.Planning;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class DataQualityTests : IntegrationTestBase
{
    public DataQualityTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private async Task<DataQualityGroupDto> GroupAsync(string key)
    {
        var result = await InScope(scope => scope.Send(new GetDataQualityQuery()));
        return result.Groups.Single(g => g.Key == key);
    }

    [Fact]
    public async Task A_worker_without_a_position_is_listed()
    {
        var before = (await GroupAsync("employeesNoPosition")).Count;
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Bez", lastName: $"Pozicije{Guid.NewGuid():N}"[..14]));
        await InScope(async scope =>
        {
            var row = await scope.Db.Employees.SingleAsync(e => e.Id == employee.Id);
            row.Position = "  ";
            await scope.Db.SaveChangesAsync();
        });

        // Counted rather than looked up: only the first 50 are listed, and a shared database can hold more.
        Assert.Equal(before + 1, (await GroupAsync("employeesNoPosition")).Count);
    }

    [Fact]
    public async Task A_vehicle_without_a_td_number_or_dates_is_listed_and_a_complete_one_is_not()
    {
        var noTdBefore = (await GroupAsync("vehiclesNoTd")).Count;
        var noDatesBefore = (await GroupAsync("vehiclesNoDates")).Count;

        var bare = await InScope(async scope =>
        {
            var vehicle = new Vehicle { Brand = "Iveco", Model = "Daily", RegistrationNumber = $"DQ-{Guid.NewGuid():N}"[..12], FuelType = FuelType.Diesel };
            scope.Db.Vehicles.Add(vehicle);
            await scope.Db.SaveChangesAsync();
            return vehicle;
        });
        var complete = await InScope(async scope =>
        {
            var vehicle = new Vehicle
            {
                Brand = "Iveco", Model = "Daily", RegistrationNumber = $"DQ-{Guid.NewGuid():N}"[..12], FuelType = FuelType.Diesel,
                TdNumber = $"TD-{Guid.NewGuid():N}"[..10],
                RegistrationValidUntil = new DateOnly(2031, 1, 1), TechnicalInspectionValidUntil = new DateOnly(2031, 1, 1), InsuranceValidUntil = new DateOnly(2031, 1, 1),
            };
            scope.Db.Vehicles.Add(vehicle);
            await scope.Db.SaveChangesAsync();
            return vehicle;
        });

        // The bare vehicle adds one to each list; the complete one adds nothing to either.
        Assert.Equal(noTdBefore + 1, (await GroupAsync("vehiclesNoTd")).Count);
        Assert.Equal(noDatesBefore + 1, (await GroupAsync("vehiclesNoDates")).Count);
        Assert.NotEqual(bare.Id, complete.Id);
    }

    [Fact]
    public async Task A_project_with_needs_stops_being_listed_as_having_none()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var withNone = (await GroupAsync("projectsNoNeeds")).Count;

        await InScopeAs(UserRole.Admin, scope => scope.Send(new SetProjectStaffingNeedsCommand { ProjectId = project.Id, Needs = [new("Zidar", 2)] }));

        Assert.Equal(withNone - 1, (await GroupAsync("projectsNoNeeds")).Count);
    }

    [Fact]
    public async Task Somebody_still_posted_to_a_finished_project_is_listed_once()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, status: ProjectStatus.Active));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var before = (await GroupAsync("postingsAfterEnd")).Count;

        await InScope(async scope =>
        {
            scope.Db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = employee.Id, ProjectId = project.Id, StartDate = today.AddDays(-10), EndDate = today.AddDays(3), AssignedAt = DateTime.UtcNow });
            scope.Db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = employee.Id, ProjectId = project.Id, StartDate = today.AddDays(5), AssignedAt = DateTime.UtcNow });
            var row = await scope.Db.Projects.SingleAsync(p => p.Id == project.Id);
            row.Status = ProjectStatus.Completed;
            await scope.Db.SaveChangesAsync();
        });

        // Two stale postings of one person are one thing to fix.
        Assert.Equal(before + 1, (await GroupAsync("postingsAfterEnd")).Count);
    }
}

[Collection(ApiCollection.Name)]
public class DataQualityHttpTests
{
    private readonly ApiFixture _api;

    public DataQualityHttpTests(ApiFixture api)
    {
        _api = api;
    }

    [Theory]
    [InlineData(UserRole.ProjectManager, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Foreman, HttpStatusCode.Forbidden)]
    [InlineData(UserRole.Admin, HttpStatusCode.OK)]
    public async Task Only_an_administrator_may_read_it(UserRole role, HttpStatusCode expected)
    {
        using var client = _api.ClientAs(role);

        var response = await client.GetAsync("/api/v1/data-quality");

        Assert.Equal(expected, response.StatusCode);
    }
}
