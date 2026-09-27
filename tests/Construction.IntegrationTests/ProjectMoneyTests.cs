using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A project's contract sum is money on a record everybody who reads projects may open, so it is
/// only shown to the finance grant, and only the grant changes it or the way the client is billed.
/// </summary>
[Collection(ApiCollection.Name)]
public class ProjectMoneyTests
{
    private readonly ApiFixture _api;

    public ProjectMoneyTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<Guid> CreateAsSuperAdminAsync(decimal? contractValue = 1234m, string billingMode = "FlatRate")
    {
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync("/api/v1/projects", new
        {
            name = $"Gradiliste {Guid.NewGuid():N}"[..20],
            contractValue,
            billingMode,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> GetAsync(HttpClient client, Guid id) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{id}");

    /// <summary>
    /// A foreman or project manager only sees a project they are posted to (SiteScope); this
    /// puts the fixture's own account of <paramref name="role"/> on the project so a check of
    /// what it may read is not confused with whether it can find the project at all.
    /// </summary>
    private async Task PostToProjectAsync(UserRole role, Guid projectId)
    {
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);
        var employeeId = await _api.InScope(db => db.Users
            .Where(u => u.Id == _api.UserIds[role])
            .Select(u => u.EmployeeId!.Value)
            .SingleAsync());

        (await superAdmin.PostAsync($"/api/v1/employees/{employeeId}/projects/{projectId}", null))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task The_super_admin_sees_the_contract_sum_and_how_the_client_is_billed()
    {
        var id = await CreateAsSuperAdminAsync();
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        var project = await GetAsync(superAdmin, id);

        Assert.Equal(1234m, project.GetProperty("contractValue").GetDecimal());
        Assert.Equal("FlatRate", project.GetProperty("billingMode").GetString());
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.ProjectManager)]
    public async Task Without_the_grant_the_contract_sum_is_hidden_but_the_billing_mode_is_not(UserRole role)
    {
        var id = await CreateAsSuperAdminAsync();
        await PostToProjectAsync(role, id);
        using var client = _api.ClientAs(role);

        var project = await GetAsync(client, id);
        Assert.Equal(JsonValueKind.Null, project.GetProperty("contractValue").ValueKind);
        Assert.Equal("FlatRate", project.GetProperty("billingMode").GetString());

        // The list too, not only the detail.
        var page = await client.GetFromJsonAsync<JsonElement>("/api/v1/projects?pageSize=100");
        var row = page.GetProperty("items").EnumerateArray().Single(e => e.GetProperty("id").GetGuid() == id);
        Assert.Equal(JsonValueKind.Null, row.GetProperty("contractValue").ValueKind);
    }

    [Fact]
    public async Task An_update_by_somebody_without_the_grant_keeps_the_contract_sum_and_the_billing_mode()
    {
        var id = await CreateAsSuperAdminAsync(contractValue: 5000m, billingMode: "Measured");
        using var manager = _api.ClientAs(UserRole.ProjectManager);

        // The form of somebody who never saw the sum sends none, and the default billing mode.
        var response = await manager.PutAsJsonAsync($"/api/v1/projects/{id}", new
        {
            name = "Preimenovano gradiliste",
            status = "Active",
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);
        var project = await GetAsync(superAdmin, id);

        Assert.Equal("Preimenovano gradiliste", project.GetProperty("name").GetString());
        Assert.Equal(5000m, project.GetProperty("contractValue").GetDecimal());
        Assert.Equal("Measured", project.GetProperty("billingMode").GetString());
    }

    [Fact]
    public async Task A_project_made_by_somebody_without_the_grant_has_no_sum_and_is_billed_by_the_hour()
    {
        using var manager = _api.ClientAs(UserRole.ProjectManager);

        var response = await manager.PostAsJsonAsync("/api/v1/projects", new
        {
            name = $"Novo gradiliste {Guid.NewGuid():N}"[..24],
            contractValue = 9999m,
            billingMode = "FlatRate",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, created.GetProperty("contractValue").ValueKind);
        Assert.Equal("Hourly", created.GetProperty("billingMode").GetString());
    }

    [Fact]
    public async Task With_the_grant_an_admin_sees_and_changes_the_sum_and_the_billing_mode()
    {
        var id = await CreateAsSuperAdminAsync();

        // A fresh admin, so the fixture's own Admin account stays without the grant for other tests.
        var (email, userId) = await _api.SeedSignInAccountAsync(UserRole.Admin);
        await _api.InScope(db => db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.FinanceAccess, FinanceAccess.Full)));

        using var admin = _api.AnonymousClient();
        admin.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await _api.SignInAsync(email));

        Assert.Equal(1234m, (await GetAsync(admin, id)).GetProperty("contractValue").GetDecimal());

        var response = await admin.PutAsJsonAsync($"/api/v1/projects/{id}", new
        {
            name = "Gradiliste sa novom cijenom",
            status = "Active",
            contractValue = 7777m,
            billingMode = "Hourly",
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var project = await GetAsync(admin, id);
        Assert.Equal(7777m, project.GetProperty("contractValue").GetDecimal());
        Assert.Equal("Hourly", project.GetProperty("billingMode").GetString());
    }
}
