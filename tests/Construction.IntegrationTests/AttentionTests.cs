using System.Net;
using System.Net.Http.Json;
using Construction.Application.Features.Attention;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class AttentionTests : IntegrationTestBase
{
    public AttentionTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private Task<AttentionDto> AsAsync(UserRole role) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(Guid.NewGuid(), role, null, "attention@example.com");
            return scope.Send(new GetAttentionQuery());
        });

    private static int Count(AttentionDto dto, string key) => dto.Groups.SingleOrDefault(g => g.Key == key)?.Count ?? 0;

    [Fact]
    public async Task An_administrator_is_told_about_a_leave_request_waiting_for_an_answer()
    {
        var before = Count(await AsAsync(UserRole.Admin), "absenceRequests");
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.AnnualLeave,
                Status = AbsenceStatus.Requested,
                StartDate = new DateOnly(2031, 5, 5),
                EndDate = new DateOnly(2031, 5, 9),
            });
            await scope.Db.SaveChangesAsync();
        });

        Assert.Equal(before + 1, Count(await AsAsync(UserRole.Admin), "absenceRequests"));
    }

    [Fact]
    public async Task A_vehicle_with_a_date_coming_up_is_listed_with_the_date_that_comes_first()
    {
        var before = Count(await AsAsync(UserRole.Admin), "vehicleDates");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await InScope(async scope =>
        {
            scope.Db.Vehicles.Add(new Vehicle
            {
                Brand = "Iveco", Model = "Daily", RegistrationNumber = $"AT-{Guid.NewGuid():N}"[..12], FuelType = FuelType.Diesel,
                RegistrationValidUntil = today.AddDays(20), InsuranceValidUntil = today.AddDays(5),
            });
            // Far enough off not to count.
            scope.Db.Vehicles.Add(new Vehicle
            {
                Brand = "Iveco", Model = "Daily", RegistrationNumber = $"AT-{Guid.NewGuid():N}"[..12], FuelType = FuelType.Diesel,
                RegistrationValidUntil = today.AddDays(200),
            });
            await scope.Db.SaveChangesAsync();
        });

        var after = await AsAsync(UserRole.Admin);

        Assert.Equal(before + 1, Count(after, "vehicleDates"));
        var first = after.Groups.Single(g => g.Key == "vehicleDates").Items.First();
        Assert.True(first.Date is not null && first.Kind is not null);
    }

    [Fact]
    public async Task A_project_manager_sees_what_they_can_act_on_and_not_what_is_an_administrators()
    {
        var manager = await AsAsync(UserRole.ProjectManager);

        Assert.DoesNotContain(manager.Groups, g => g.Key is "absenceRequests" or "dkvRows" or "vehicleDates" or "dataQuality" or "articleOrders" or "refunds");
    }

    [Fact]
    public async Task A_worker_is_told_nothing()
    {
        Assert.Empty((await AsAsync(UserRole.Worker)).Groups);
    }

    [Fact]
    public async Task A_posting_the_worker_has_not_confirmed_is_counted_for_a_manager_and_stops_once_confirmed()
    {
        var before = Count(await AsAsync(UserRole.ProjectManager), "unconfirmedPostings");
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await InScope(async scope =>
        {
            scope.Db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = worker.Id, ProjectId = site.Id, StartDate = today.AddDays(2), AssignedAt = DateTime.UtcNow });
            await scope.Db.SaveChangesAsync();
        });

        Assert.Equal(before + 1, Count(await AsAsync(UserRole.ProjectManager), "unconfirmedPostings"));

        await InScope(async scope =>
        {
            var row = await scope.Db.EmployeeProjects.SingleAsync(ep => ep.EmployeeId == worker.Id);
            row.AcknowledgedAt = DateTime.UtcNow;
            await scope.Db.SaveChangesAsync();
        });

        Assert.Equal(before, Count(await AsAsync(UserRole.ProjectManager), "unconfirmedPostings"));
    }
}

[Collection(ApiCollection.Name)]
public class AttentionHttpTests
{
    private readonly ApiFixture _api;

    public AttentionHttpTests(ApiFixture api)
    {
        _api = api;
    }

    [Theory]
    [InlineData(UserRole.Worker, HttpStatusCode.OK)]
    [InlineData(UserRole.Admin, HttpStatusCode.OK)]
    [InlineData(UserRole.Customer, HttpStatusCode.Forbidden)]
    public async Task Every_employee_may_ask_and_a_customer_may_not(UserRole role, HttpStatusCode expected)
    {
        using var client = _api.ClientAs(role);

        var response = await client.GetAsync("/api/v1/attention");

        Assert.Equal(expected, response.StatusCode);
    }
}
