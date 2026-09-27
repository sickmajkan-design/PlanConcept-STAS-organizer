using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The customer's answers of 26–27 September 2026, exercised through the real
/// HTTP pipeline with real sign-in tokens: who decides leave and refunds, who
/// is told, and who may see figures in euro.
/// </summary>
[Collection(ApiCollection.Name)]
public class CustomerRulesEndToEndTests
{
    private readonly ApiFixture _api;

    public CustomerRulesEndToEndTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<(HttpClient Client, Guid UserId)> FreshAccountAsync(UserRole role)
    {
        var (email, userId) = await _api.SeedSignInAccountAsync(role);
        var token = await _api.SignInAsync(email);
        var client = _api.AnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    private static string Day(int offset) =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(offset).ToString("yyyy-MM-dd");

    private static int _nextSlot = 40;

    /// <summary>Distinct leave dates per call, so overlap rules never interfere.</summary>
    private static (string Start, string End) NextLeave()
    {
        var slot = Interlocked.Add(ref _nextSlot, 4);

        return (Day(slot), Day(slot + 1));
    }

    private static async Task<JsonElement> RequestLeaveAsync(HttpClient client, object? extra = null)
    {
        var (start, end) = NextLeave();
        var body = new Dictionary<string, object?>
        {
            ["type"] = "AnnualLeave",
            ["startDate"] = start,
            ["endDate"] = end,
            ["reason"] = "Odmor",
        };

        if (extra is not null)
        {
            foreach (var property in extra.GetType().GetProperties())
            {
                body[property.Name] = property.GetValue(extra);
            }
        }

        var response = await client.PostAsJsonAsync("/api/absences", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<HttpResponseMessage> Review(HttpClient client, Guid id, bool approve, string? note = null) =>
        client.PostAsJsonAsync($"/api/absences/{id}/review", new { approve, note });

    private Task<int> NotificationsAsync(Guid userId, NotificationType type) =>
        _api.InScope(db => db.Notifications.CountAsync(n => n.UserId == userId && n.Type == type));

    // ---------------------------------------------------------------- leave

    [Fact]
    public async Task A_workers_leave_request_is_decided_by_an_admin_and_by_nobody_below()
    {
        var (worker, _) = await FreshAccountAsync(UserRole.Worker);
        var leave = await RequestLeaveAsync(worker);
        var id = leave.GetProperty("id").GetGuid();
        Assert.Equal("Requested", leave.GetProperty("status").GetString());

        foreach (var role in new[] { UserRole.Worker, UserRole.Foreman, UserRole.ProjectManager })
        {
            using var below = _api.ClientAs(role);
            Assert.Equal(HttpStatusCode.Forbidden, (await Review(below, id, approve: true)).StatusCode);
        }

        // Still waiting: nothing below management changed it.
        using var admin = _api.ClientAs(UserRole.Admin);
        var approved = await Review(admin, id, approve: true);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal("Approved", (await approved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Refusing_leave_needs_a_reason_and_a_super_admin_may_refuse()
    {
        var (worker, _) = await FreshAccountAsync(UserRole.Worker);
        var id = (await RequestLeaveAsync(worker)).GetProperty("id").GetGuid();
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        Assert.Equal(HttpStatusCode.BadRequest, (await Review(superAdmin, id, approve: false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Review(superAdmin, id, approve: false, "Nema ljudi")).StatusCode);

        // Existing behaviour, unchanged: management may reconsider a refusal.
        Assert.Equal(HttpStatusCode.OK, (await Review(superAdmin, id, approve: true)).StatusCode);
    }

    [Fact]
    public async Task Nobody_grants_their_own_leave_not_even_an_admin()
    {
        var (admin, _) = await FreshAccountAsync(UserRole.Admin);
        var id = (await RequestLeaveAsync(admin)).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await Review(admin, id, approve: true)).StatusCode);
    }

    [Fact]
    public async Task A_foreman_may_record_leave_for_somebody_but_not_grant_it()
    {
        var (foreman, _) = await FreshAccountAsync(UserRole.Foreman);
        var (_, workerId) = await FreshAccountAsync(UserRole.Worker);
        var employeeId = await _api.InScope(db =>
            db.Users.Where(u => u.Id == workerId).Select(u => u.EmployeeId).SingleAsync());

        var recorded = await RequestLeaveAsync(foreman, new { EmployeeId = employeeId });
        Assert.Equal("Requested", recorded.GetProperty("status").GetString());

        var (start, end) = NextLeave();
        var granted = await foreman.PostAsJsonAsync("/api/absences", new
        {
            employeeId,
            type = "AnnualLeave",
            startDate = start,
            endDate = end,
            approve = true,
        });
        Assert.Equal(HttpStatusCode.Forbidden, granted.StatusCode);
    }

    [Fact]
    public async Task A_worker_cannot_record_leave_for_somebody_else()
    {
        var (worker, _) = await FreshAccountAsync(UserRole.Worker);
        var (_, otherId) = await FreshAccountAsync(UserRole.Worker);
        var otherEmployee = await _api.InScope(db =>
            db.Users.Where(u => u.Id == otherId).Select(u => u.EmployeeId).SingleAsync());
        var (start, end) = NextLeave();

        var response = await worker.PostAsJsonAsync("/api/absences", new
        {
            employeeId = otherEmployee,
            type = "AnnualLeave",
            startDate = start,
            endDate = end,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Only_management_is_told_about_a_new_leave_request()
    {
        var before = new Dictionary<UserRole, int>();
        foreach (var role in new[] { UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager, UserRole.Foreman })
        {
            before[role] = await NotificationsAsync(_api.UserIds[role], NotificationType.AbsenceRequested);
        }

        var (worker, _) = await FreshAccountAsync(UserRole.Worker);
        await RequestLeaveAsync(worker);

        Assert.True(await NotificationsAsync(_api.UserIds[UserRole.SuperAdmin], NotificationType.AbsenceRequested) > before[UserRole.SuperAdmin]);
        Assert.True(await NotificationsAsync(_api.UserIds[UserRole.Admin], NotificationType.AbsenceRequested) > before[UserRole.Admin]);
        Assert.Equal(before[UserRole.ProjectManager], await NotificationsAsync(_api.UserIds[UserRole.ProjectManager], NotificationType.AbsenceRequested));
        Assert.Equal(before[UserRole.Foreman], await NotificationsAsync(_api.UserIds[UserRole.Foreman], NotificationType.AbsenceRequested));
    }

    [Fact]
    public async Task The_person_is_told_the_answer()
    {
        var (worker, workerUserId) = await FreshAccountAsync(UserRole.Worker);
        var id = (await RequestLeaveAsync(worker)).GetProperty("id").GetGuid();
        var before = await NotificationsAsync(workerUserId, NotificationType.AbsenceDecided);

        using var admin = _api.ClientAs(UserRole.Admin);
        await Review(admin, id, approve: true);

        Assert.Equal(before + 1, await NotificationsAsync(workerUserId, NotificationType.AbsenceDecided));
    }

    // -------------------------------------------------------------- refunds

    private static object RefundBody() => new
    {
        amount = 18.9m,
        currency = "EUR",
        expenseDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        description = "Radne rukavice",
    };

    [Fact]
    public async Task A_refund_is_decided_by_an_admin_and_by_nobody_below()
    {
        var (worker, _) = await FreshAccountAsync(UserRole.Worker);
        var created = await worker.PostAsJsonAsync("/api/v1/refunds", RefundBody());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        foreach (var role in new[] { UserRole.Foreman, UserRole.ProjectManager })
        {
            using var below = _api.ClientAs(role);
            var refused = await below.PostAsJsonAsync($"/api/v1/refunds/{id}/review", new { status = "Approved" });
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        }

        using var admin = _api.ClientAs(UserRole.Admin);
        var approved = await admin.PostAsJsonAsync(
            $"/api/v1/refunds/{id}/review", new { status = "Approved", payrollYear = 2026, payrollMonth = 10 });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
    }

    [Fact]
    public async Task Only_management_sees_everybodys_refunds_and_is_told_of_a_new_one()
    {
        var before = new Dictionary<UserRole, int>();
        foreach (var role in new[] { UserRole.SuperAdmin, UserRole.Admin, UserRole.ProjectManager })
        {
            before[role] = await NotificationsAsync(_api.UserIds[role], NotificationType.RefundRequested);
        }

        var (worker, _) = await FreshAccountAsync(UserRole.Worker);
        var created = await worker.PostAsJsonAsync("/api/v1/refunds", RefundBody());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.True(await NotificationsAsync(_api.UserIds[UserRole.Admin], NotificationType.RefundRequested) > before[UserRole.Admin]);
        Assert.Equal(before[UserRole.ProjectManager], await NotificationsAsync(_api.UserIds[UserRole.ProjectManager], NotificationType.RefundRequested));

        // A project manager now sees only what they asked for themselves.
        using var manager = _api.ClientAs(UserRole.ProjectManager);
        var page = await manager.GetFromJsonAsync<JsonElement>("/api/v1/refunds?pageSize=100");
        Assert.DoesNotContain(page.GetProperty("items").EnumerateArray(), e => e.GetProperty("id").GetGuid() == id);

        using var admin = _api.ClientAs(UserRole.Admin);
        var all = await admin.GetFromJsonAsync<JsonElement>("/api/v1/refunds?pageSize=100");
        Assert.Contains(all.GetProperty("items").EnumerateArray(), e => e.GetProperty("id").GetGuid() == id);
    }

    // ------------------------------------------------ figures in euro (grant)

    private async Task<HttpStatusCode> UpdateAsync(HttpClient superAdmin, Guid userId, UserRole role, string financeAccess)
    {
        var employeeId = await _api.InScope(db =>
            db.Users.Where(u => u.Id == userId).Select(u => u.EmployeeId).SingleAsync());
        var email = await _api.InScope(db =>
            db.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync());

        var response = await superAdmin.PutAsJsonAsync($"/api/users/{userId}", new
        {
            email,
            role = role.ToString(),
            employeeId,
            financeAccess,
        });

        return response.StatusCode;
    }

    [Fact]
    public async Task A_grant_takes_effect_on_the_next_request_and_a_revoke_too_with_the_same_token()
    {
        var (foreman, foremanId) = await FreshAccountAsync(UserRole.Foreman);
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        Assert.Equal(HttpStatusCode.Forbidden, (await foreman.GetAsync("/api/material-movements")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, await UpdateAsync(superAdmin, foremanId, UserRole.Foreman, "Full"));
        Assert.Equal(HttpStatusCode.OK, (await foreman.GetAsync("/api/material-movements")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await foreman.GetAsync("/api/vehicle-expenses")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, await UpdateAsync(superAdmin, foremanId, UserRole.Foreman, "None"));
        Assert.Equal(HttpStatusCode.Forbidden, (await foreman.GetAsync("/api/material-movements")).StatusCode);
    }

    [Fact]
    public async Task Statistics_only_shows_no_amounts()
    {
        var (admin, adminId) = await FreshAccountAsync(UserRole.Admin);
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        Assert.Equal(HttpStatusCode.OK, await UpdateAsync(superAdmin, adminId, UserRole.Admin, "StatisticsOnly"));

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/material-movements")).StatusCode);
    }

    [Fact]
    public async Task The_grant_cannot_be_given_to_a_worker_over_the_wire()
    {
        var (worker, workerId) = await FreshAccountAsync(UserRole.Worker);
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        Assert.Equal(HttpStatusCode.Conflict, await UpdateAsync(superAdmin, workerId, UserRole.Worker, "Full"));
        Assert.Equal(HttpStatusCode.Forbidden, (await worker.GetAsync("/api/material-movements")).StatusCode);
    }

    [Fact]
    public async Task An_admin_cannot_hand_out_the_grant()
    {
        var (_, foremanId) = await FreshAccountAsync(UserRole.Foreman);
        var (foreman, _) = (await FreshAccountAsync(UserRole.Foreman));
        using var admin = _api.ClientAs(UserRole.Admin);

        // The request is accepted (an admin may edit the account) but the grant is ignored.
        await UpdateAsync(admin, foremanId, UserRole.Foreman, "Full");

        var stored = await _api.InScope(db =>
            db.Users.Where(u => u.Id == foremanId).Select(u => u.FinanceAccess).SingleAsync());
        Assert.Equal(FinanceAccess.None, stored);
        Assert.Equal(HttpStatusCode.Forbidden, (await foreman.GetAsync("/api/material-movements")).StatusCode);
    }

    [Fact]
    public async Task Demoting_a_granted_foreman_to_worker_removes_the_grant()
    {
        var (_, foremanId) = await FreshAccountAsync(UserRole.Foreman);
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);
        await UpdateAsync(superAdmin, foremanId, UserRole.Foreman, "Full");

        Assert.Equal(HttpStatusCode.OK, await UpdateAsync(superAdmin, foremanId, UserRole.Worker, "None"));

        var stored = await _api.InScope(db =>
            db.Users.Where(u => u.Id == foremanId).Select(u => u.FinanceAccess).SingleAsync());
        Assert.Equal(FinanceAccess.None, stored);
    }

    [Fact]
    public async Task A_customer_login_gets_no_figures_at_all()
    {
        using var customer = _api.ClientAs(UserRole.Customer);

        Assert.True((await customer.GetAsync("/api/material-movements")).StatusCode
            is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
        Assert.True((await customer.GetAsync("/api/employee-rates")).StatusCode
            is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/employee-rates")]
    [InlineData("/api/employee-rates/summary")]
    public async Task Pay_rates_need_the_grant_even_for_a_project_manager(string path)
    {
        var (manager, managerId) = await FreshAccountAsync(UserRole.ProjectManager);
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync(path)).StatusCode);

        await UpdateAsync(superAdmin, managerId, UserRole.ProjectManager, "Full");
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(path)).StatusCode);

        // A foreman with the grant still never sees pay rates: the role ceiling stays.
        var (foreman, foremanId) = await FreshAccountAsync(UserRole.Foreman);
        await UpdateAsync(superAdmin, foremanId, UserRole.Foreman, "Full");
        Assert.Equal(HttpStatusCode.Forbidden, (await foreman.GetAsync(path)).StatusCode);
    }
}
