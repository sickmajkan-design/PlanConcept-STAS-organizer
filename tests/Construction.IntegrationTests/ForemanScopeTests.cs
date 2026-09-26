using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A foreman runs a site, not the company: the project and employee directories
/// show them only the sites they are posted to and the people on those sites.
/// </summary>
[Collection(ApiCollection.Name)]
public class ForemanScopeTests
{
    private readonly ApiFixture _api;

    public ForemanScopeTests(ApiFixture api)
    {
        _api = api;
    }

    private sealed record Ids(Guid OwnSite, Guid OtherSite, Guid Crew, Guid Stranger);

    private async Task<Guid> ForemanEmployeeIdAsync()
    {
        var userId = _api.UserIds[UserRole.Foreman];

        return await _api.InScope(context => context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.EmployeeId!.Value)
            .SingleAsync());
    }

    private static async Task<Guid> CreateAsync(HttpClient admin, string path, object body)
    {
        var response = await admin.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Two sites; the foreman and a crew member on the first, somebody else
    /// entirely on the second.
    /// </summary>
    private async Task<Ids> SeedAsync()
    {
        using var admin = _api.ClientAs(UserRole.SuperAdmin);
        var tag = Guid.NewGuid().ToString("N")[..10];

        var ownSite = await CreateAsync(admin, "/api/v1/projects",
            new { name = $"Own {tag}", client = "C", startDate = "2024-01-01" });
        var otherSite = await CreateAsync(admin, "/api/v1/projects",
            new { name = $"Other {tag}", client = "C", startDate = "2024-01-01" });

        var crew = await CreateAsync(admin, "/api/v1/employees", new
        {
            employeeNumber = $"CR-{tag}", firstName = "Crew", lastName = $"Member{tag}",
            position = "Zidar", employmentDate = "2024-01-15",
        });
        var stranger = await CreateAsync(admin, "/api/v1/employees", new
        {
            employeeNumber = $"ST-{tag}", firstName = "Stranger", lastName = $"Elsewhere{tag}",
            position = "Zidar", employmentDate = "2024-01-15",
        });

        var foreman = await ForemanEmployeeIdAsync();

        (await admin.PostAsync($"/api/v1/employees/{foreman}/projects/{ownSite}", null)).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/v1/employees/{crew}/projects/{ownSite}", null)).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/v1/employees/{stranger}/projects/{otherSite}", null)).EnsureSuccessStatusCode();

        return new Ids(ownSite, otherSite, crew, stranger);
    }

    private static async Task<HashSet<Guid>> IdsOfAsync(HttpClient client, string path)
    {
        var page = await client.GetFromJsonAsync<JsonElement>(path);

        return page.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToHashSet();
    }

    [Fact]
    public async Task A_foreman_lists_only_the_sites_they_are_posted_to()
    {
        var ids = await SeedAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);

        var visible = await IdsOfAsync(foreman, "/api/v1/projects?pageSize=100");

        Assert.Contains(ids.OwnSite, visible);
        Assert.DoesNotContain(ids.OtherSite, visible);
    }

    [Fact]
    public async Task A_foreman_lists_only_the_people_on_their_sites_and_themselves()
    {
        var ids = await SeedAsync();
        var self = await ForemanEmployeeIdAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);

        var visible = await IdsOfAsync(foreman, "/api/v1/employees?pageSize=100");

        Assert.Contains(ids.Crew, visible);
        Assert.Contains(self, visible);
        Assert.DoesNotContain(ids.Stranger, visible);
    }

    [Fact]
    public async Task A_site_or_person_outside_the_foremans_sites_is_not_found_not_forbidden()
    {
        var ids = await SeedAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);

        // Not told that it exists.
        Assert.Equal(HttpStatusCode.NotFound,
            (await foreman.GetAsync($"/api/v1/projects/{ids.OtherSite}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await foreman.GetAsync($"/api/v1/employees/{ids.Stranger}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await foreman.GetAsync($"/api/v1/projects/{ids.OwnSite}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await foreman.GetAsync($"/api/v1/employees/{ids.Crew}")).StatusCode);
    }

    [Fact]
    public async Task A_posting_that_has_ended_no_longer_shows_the_site()
    {
        var ids = await SeedAsync();
        var self = await ForemanEmployeeIdAsync();

        using var admin = _api.ClientAs(UserRole.SuperAdmin);
        (await admin.DeleteAsync($"/api/v1/employees/{self}/projects/{ids.OwnSite}")).EnsureSuccessStatusCode();

        using var foreman = _api.ClientAs(UserRole.Foreman);

        Assert.DoesNotContain(ids.OwnSite, await IdsOfAsync(foreman, "/api/v1/projects?pageSize=100"));
    }

    [Fact]
    public async Task A_foreman_sees_the_work_on_their_own_sites_only()
    {
        var ids = await SeedAsync();
        using var admin = _api.ClientAs(UserRole.SuperAdmin);

        var onOwn = await CreateAsync(admin, "/api/v1/workitems",
            new { kind = "Task", title = $"Own {Guid.NewGuid():N}"[..14], projectId = ids.OwnSite, priority = "Normal" });
        var onOther = await CreateAsync(admin, "/api/v1/workitems",
            new { kind = "Task", title = $"Other {Guid.NewGuid():N}"[..14], projectId = ids.OtherSite, priority = "Normal" });

        using var foreman = _api.ClientAs(UserRole.Foreman);

        var visible = await IdsOfAsync(foreman, "/api/v1/workitems?pageSize=100");

        Assert.Contains(onOwn, visible);
        Assert.DoesNotContain(onOther, visible);

        Assert.Equal(HttpStatusCode.OK, (await foreman.GetAsync($"/api/v1/workitems/{onOwn}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await foreman.GetAsync($"/api/v1/workitems/{onOther}")).StatusCode);
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.ProjectManager)]
    public async Task The_office_roles_still_see_everything(UserRole role)
    {
        var ids = await SeedAsync();
        using var client = _api.ClientAs(role);

        var projects = await IdsOfAsync(client, "/api/v1/projects?pageSize=100");
        var employees = await IdsOfAsync(client, "/api/v1/employees?pageSize=100");

        Assert.Contains(ids.OwnSite, projects);
        Assert.Contains(ids.OtherSite, projects);
        Assert.Contains(ids.Stranger, employees);
    }
}
