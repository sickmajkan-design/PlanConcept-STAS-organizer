using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The leave rules through the real HTTP pipeline: the balance a person sees, corrections by
/// management only, and the firm's leave settings, whose amount needs the finance grant.
/// </summary>
[Collection(ApiCollection.Name)]
public class LeaveEndpointsTests
{
    private readonly ApiFixture _api;

    public LeaveEndpointsTests(ApiFixture api)
    {
        _api = api;
    }

    private Guid EmployeeOf(UserRole role) =>
        _api.InScope(db => db.Users.Where(u => u.Id == _api.UserIds[role]).Select(u => u.EmployeeId!.Value).SingleAsync())
            .GetAwaiter().GetResult();

    [Fact]
    public async Task A_worker_reads_their_own_balance_with_the_new_breakdown()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        var employeeId = EmployeeOf(UserRole.Worker);

        var response = await worker.GetAsync($"/api/v1/absences/balance?employeeId={employeeId}&year=2026");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(20, body.GetProperty("entitlementDays").GetInt32());
        Assert.Equal("2026-06-01", body.GetProperty("carryOverExpiresOn").GetString());
        Assert.True(body.TryGetProperty("carriedOverDays", out _));
        Assert.True(body.TryGetProperty("remainingDays", out _));
    }

    [Fact]
    public async Task Management_writes_a_correction_over_http_and_it_shows_in_the_balance_and_history()
    {
        using var admin = _api.ClientAs(UserRole.Admin);
        var employeeId = EmployeeOf(UserRole.Foreman);

        var created = await admin.PostAsJsonAsync("/api/v1/absences/adjustments", new
        {
            employeeId, year = 2031, days = 4, reason = "Prenos iz stare evidencije",
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var balance = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/absences/balance?employeeId={employeeId}&year=2031");
        Assert.Equal(4, balance.GetProperty("adjustmentDays").GetInt32());

        var history = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/absences/adjustments?employeeId={employeeId}&year=2031");
        Assert.Equal(1, history.GetArrayLength());
        Assert.Equal("Admin@api-tests.test".ToLowerInvariant(), history[0].GetProperty("createdBy").GetString());
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    [InlineData(UserRole.Worker)]
    public async Task Below_management_a_correction_is_refused(UserRole role)
    {
        using var client = _api.ClientAs(role);

        var response = await client.PostAsJsonAsync("/api/v1/absences/adjustments", new
        {
            employeeId = EmployeeOf(role), year = 2026, days = 5, reason = "Zelim vise",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_correction_with_zero_days_or_no_reason_is_a_bad_request()
    {
        using var admin = _api.ClientAs(UserRole.Admin);
        var employeeId = EmployeeOf(UserRole.Foreman);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(
            "/api/v1/absences/adjustments", new { employeeId, year = 2026, days = 0, reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(
            "/api/v1/absences/adjustments", new { employeeId, year = 2026, days = 2, reason = "" })).StatusCode);
    }

    [Fact]
    public async Task The_leave_amount_is_money_so_an_admin_without_the_grant_is_refused_and_the_super_admin_is_not()
    {
        using var admin = _api.ClientAs(UserRole.Admin);
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);
        using var foreman = _api.ClientAs(UserRole.Foreman);

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/leave-settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await foreman.GetAsync("/api/v1/leave-settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync(
            "/api/v1/leave-settings", new { annualLeaveDailyRate = 32m, holidayCountryCode = "DE" })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await superAdmin.GetAsync("/api/v1/leave-settings")).StatusCode);
    }

    [Fact]
    public async Task The_super_admin_sets_the_amount_and_the_country_once_the_company_profile_exists()
    {
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        try
        {
            // Nowhere to keep it until the company profile has been saved once.
            await _api.InScope(db => db.CompanySettings.ExecuteDeleteAsync());
            Assert.Equal(HttpStatusCode.Conflict, (await superAdmin.PutAsJsonAsync(
                "/api/v1/leave-settings", new { annualLeaveDailyRate = 32m, holidayCountryCode = "DE" })).StatusCode);

            await _api.InScope(async db =>
            {
                db.CompanySettings.Add(new Construction.Domain.Entities.CompanySettings { Name = "Test firm" });
                await db.SaveChangesAsync();
                return 0;
            });

            var saved = await superAdmin.PutAsJsonAsync(
                "/api/v1/leave-settings", new { annualLeaveDailyRate = 32m, holidayCountryCode = "de" });
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

            var read = await superAdmin.GetFromJsonAsync<JsonElement>("/api/v1/leave-settings");
            Assert.Equal(32m, read.GetProperty("annualLeaveDailyRate").GetDecimal());
            Assert.Equal("DE", read.GetProperty("holidayCountryCode").GetString());

            Assert.Equal(HttpStatusCode.BadRequest, (await superAdmin.PutAsJsonAsync(
                "/api/v1/leave-settings", new { annualLeaveDailyRate = -1m, holidayCountryCode = "DE" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await superAdmin.PutAsJsonAsync(
                "/api/v1/leave-settings", new { annualLeaveDailyRate = 32m, holidayCountryCode = "Deutschland" })).StatusCode);
        }
        finally
        {
            await _api.InScope(db => db.CompanySettings.ExecuteDeleteAsync());
        }
    }

    [Fact]
    public async Task The_company_profile_never_carries_the_leave_amount_to_ordinary_users()
    {
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);
        using var worker = _api.ClientAs(UserRole.Worker);

        try
        {
            await _api.InScope(db => db.CompanySettings.ExecuteDeleteAsync());
            await _api.InScope(async db =>
            {
                db.CompanySettings.Add(new Construction.Domain.Entities.CompanySettings
                {
                    Name = "Test firm", AnnualLeaveDailyRate = 32m, LeaveHolidayCountryCode = "DE",
                });
                await db.SaveChangesAsync();
                return 0;
            });

            // The profile is not open to a worker at all, and even the office's copy of it is not where
            // the amount lives: it has its own endpoint behind the finance grant.
            Assert.Equal(HttpStatusCode.Forbidden, (await worker.GetAsync("/api/v1/company-settings")).StatusCode);

            var profile = await superAdmin.GetStringAsync("/api/v1/company-settings");

            Assert.DoesNotContain("annualLeaveDailyRate", profile, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("leaveHolidayCountryCode", profile, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await _api.InScope(db => db.CompanySettings.ExecuteDeleteAsync());
        }
    }
}
