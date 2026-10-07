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
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Bez", lastName: $"Pozicije{Guid.NewGuid():N}"[..14]));
        await InScope(async scope =>
        {
            var row = await scope.Db.Employees.SingleAsync(e => e.Id == employee.Id);
            row.Position = "  ";
            await scope.Db.SaveChangesAsync();
        });

        var group = await GroupAsync("employeesNoPosition");

        Assert.Contains(group.Items, i => i.Id == employee.Id);
        Assert.True(group.Count >= 1);
    }

    [Fact]
    public async Task A_vehicle_without_a_td_number_or_dates_is_listed_and_a_complete_one_is_not()
    {
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

        var noTd = await GroupAsync("vehiclesNoTd");
        var noDates = await GroupAsync("vehiclesNoDates");

        Assert.Contains(noTd.Items, i => i.Id == bare.Id);
        Assert.DoesNotContain(noTd.Items, i => i.Id == complete.Id);
        Assert.Contains(noDates.Items, i => i.Id == bare.Id && i.Detail!.Contains("registration") && i.Detail.Contains("insurance"));
        Assert.DoesNotContain(noDates.Items, i => i.Id == complete.Id);
    }

    [Fact]
    public async Task A_project_with_needs_stops_being_listed_as_having_none()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        Assert.Contains((await GroupAsync("projectsNoNeeds")).Items, i => i.Id == project.Id);

        await InScope(scope => scope.Send(new SetProjectStaffingNeedsCommand { ProjectId = project.Id, Needs = [new("Zidar", 2)] }));

        Assert.DoesNotContain((await GroupAsync("projectsNoNeeds")).Items, i => i.Id == project.Id);
    }

    [Fact]
    public async Task Somebody_still_posted_to_a_finished_project_is_listed_once()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, status: ProjectStatus.Active));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await InScope(async scope =>
        {
            scope.Db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = employee.Id, ProjectId = project.Id, StartDate = today.AddDays(-10), EndDate = today.AddDays(3), AssignedAt = DateTime.UtcNow });
            scope.Db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = employee.Id, ProjectId = project.Id, StartDate = today.AddDays(5), AssignedAt = DateTime.UtcNow });
            var row = await scope.Db.Projects.SingleAsync(p => p.Id == project.Id);
            row.Status = ProjectStatus.Completed;
            await scope.Db.SaveChangesAsync();
        });

        var group = await GroupAsync("postingsAfterEnd");

        Assert.Single(group.Items, i => i.Id == employee.Id);
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
