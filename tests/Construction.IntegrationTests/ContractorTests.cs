using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>A subcontractor works under a contract: the firm and contacts are required, and they are always a worker.</summary>
[Collection(ApiCollection.Name)]
public class ContractorTests
{
    private readonly ApiFixture _api;

    public ContractorTests(ApiFixture api)
    {
        _api = api;
    }

    private static object Contractor(string number, object? extra = null)
    {
        var fields = new Dictionary<string, object?>
        {
            ["employeeNumber"] = number,
            ["firstName"] = "Kenan",
            ["lastName"] = "Kooperant",
            ["position"] = "Fasader",
            ["employmentDate"] = "2026-11-01",
            ["status"] = "Active",
            ["type"] = "Subcontractor",
            ["phone"] = "+38765111222",
            ["email"] = $"{number.ToLowerInvariant()}@firma.ba",
            ["contractorCompany"] = "Zidar d.o.o.",
        };

        if (extra is not null)
        {
            foreach (var p in extra.GetType().GetProperties())
            {
                fields[char.ToLowerInvariant(p.Name[0]) + p.Name[1..]] = p.GetValue(extra);
            }
        }

        return fields;
    }

    private static string Number() => "K-" + Guid.NewGuid().ToString("N")[..8];

    private async Task<Guid> CreateBranchAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/branches", new { name = "Unit " + Guid.NewGuid().ToString("N")[..6], color = "#3457D5" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task A_contractor_needs_the_firm_phone_email_and_a_business_unit()
    {
        using var admin = _api.ClientAs(UserRole.Admin);
        var branch = await CreateBranchAsync(admin);

        var noFirm = await admin.PostAsJsonAsync("/api/v1/employees", Contractor(Number(), new { ContractorCompany = (string?)null, BranchId = branch }));
        var noPhone = await admin.PostAsJsonAsync("/api/v1/employees", Contractor(Number(), new { Phone = (string?)null, BranchId = branch }));
        var noUnit = await admin.PostAsJsonAsync("/api/v1/employees", Contractor(Number()));

        Assert.Equal(HttpStatusCode.BadRequest, noFirm.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noPhone.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noUnit.StatusCode);
    }

    [Fact]
    public async Task The_engagement_cannot_end_before_it_starts_and_a_valid_contractor_is_saved()
    {
        using var admin = _api.ClientAs(UserRole.Admin);
        var branch = await CreateBranchAsync(admin);

        var backwards = await admin.PostAsJsonAsync("/api/v1/employees", Contractor(Number(), new { BranchId = branch, EngagementEndDate = "2026-10-01" }));
        var ok = await admin.PostAsJsonAsync("/api/v1/employees", Contractor(Number(), new { BranchId = branch, EngagementEndDate = "2027-03-01" }));

        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);

        var saved = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Zidar d.o.o.", saved.GetProperty("contractorCompany").GetString());
        Assert.Equal("2027-03-01", saved.GetProperty("engagementEndDate").GetString());
        Assert.Equal(branch, saved.GetProperty("branchId").GetGuid());
    }

    [Fact]
    public async Task A_contractor_account_can_only_be_a_worker_and_the_rank_only_worker()
    {
        using var admin = _api.ClientAs(UserRole.Admin);
        var branch = await CreateBranchAsync(admin);
        var created = await admin.PostAsJsonAsync("/api/v1/employees", Contractor(Number(), new { BranchId = branch }));
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var asForeman = await admin.PostAsJsonAsync("/api/v1/users", new
        {
            email = $"{Guid.NewGuid():N}@firma.ba",
            password = "Kooperant#Pass2026!x",
            role = "Foreman",
            employeeId = id,
        });
        var highRank = await admin.PutAsJsonAsync($"/api/v1/employees/{id}/rank", new { rank = "Foreman" });
        var workerRank = await admin.PutAsJsonAsync($"/api/v1/employees/{id}/rank", new { rank = "Worker" });

        Assert.Equal(HttpStatusCode.Conflict, asForeman.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, highRank.StatusCode);
        Assert.Equal(HttpStatusCode.OK, workerRank.StatusCode);
    }

    [Fact]
    public async Task An_update_from_a_client_that_does_not_know_the_contract_fields_keeps_them()
    {
        using var admin = _api.ClientAs(UserRole.Admin);
        var branch = await CreateBranchAsync(admin);
        var number = Number();
        var created = await admin.PostAsJsonAsync("/api/v1/employees", Contractor(number, new { BranchId = branch, EngagementEndDate = "2027-03-01" }));
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // What the installed phone app sends: no firm and no end of engagement.
        var legacy = await admin.PutAsJsonAsync($"/api/v1/employees/{id}", new
        {
            employeeNumber = number,
            firstName = "Kenan",
            lastName = "Novo",
            position = "Fasader",
            employmentDate = "2026-11-01",
            status = "Active",
            type = "Subcontractor",
            phone = "+38765111222",
            email = $"{number.ToLowerInvariant()}@firma.ba",
        });
        legacy.EnsureSuccessStatusCode();
        var kept = await legacy.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Novo", kept.GetProperty("lastName").GetString());
        Assert.Equal("Zidar d.o.o.", kept.GetProperty("contractorCompany").GetString());
        Assert.Equal("2027-03-01", kept.GetProperty("engagementEndDate").GetString());

        // The panel sends the firm, so an empty end really clears it.
        var cleared = await admin.PutAsJsonAsync($"/api/v1/employees/{id}", Contractor(number, new { EngagementEndDate = (string?)null }));
        cleared.EnsureSuccessStatusCode();
        var open = await cleared.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Null, open.GetProperty("engagementEndDate").ValueKind);
    }
}
