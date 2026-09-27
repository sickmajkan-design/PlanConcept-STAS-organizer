using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A foreman or a project manager runs a site, not the company — the customer's own words for
/// the latter: sees the project they are posted to, its fleet and its roster, "and the like".
/// The project and employee directories show either role only the sites they are posted to and
/// the people on those sites.
/// </summary>
[Collection(ApiCollection.Name)]
public class SiteScopeTests
{
    private readonly ApiFixture _api;

    public SiteScopeTests(ApiFixture api)
    {
        _api = api;
    }

    public static TheoryData<UserRole> ScopedRoles => new() { UserRole.Foreman, UserRole.ProjectManager };

    private sealed record Ids(Guid OwnSite, Guid OtherSite, Guid Crew, Guid Stranger);

    private async Task<Guid> EmployeeIdOfAsync(UserRole role)
    {
        var userId = _api.UserIds[role];

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
    /// Two sites; the scoped account and a crew member on the first, somebody else
    /// entirely on the second.
    /// </summary>
    private async Task<Ids> SeedAsync(UserRole role)
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

        var scoped = await EmployeeIdOfAsync(role);

        (await admin.PostAsync($"/api/v1/employees/{scoped}/projects/{ownSite}", null)).EnsureSuccessStatusCode();
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

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task Lists_only_the_sites_they_are_posted_to(UserRole role)
    {
        var ids = await SeedAsync(role);
        using var client = _api.ClientAs(role);

        var visible = await IdsOfAsync(client, "/api/v1/projects?pageSize=100");

        Assert.Contains(ids.OwnSite, visible);
        Assert.DoesNotContain(ids.OtherSite, visible);
    }

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task Lists_only_the_people_on_their_sites_and_themselves(UserRole role)
    {
        var ids = await SeedAsync(role);
        var self = await EmployeeIdOfAsync(role);
        using var client = _api.ClientAs(role);

        var visible = await IdsOfAsync(client, "/api/v1/employees?pageSize=100");

        Assert.Contains(ids.Crew, visible);
        Assert.Contains(self, visible);
        Assert.DoesNotContain(ids.Stranger, visible);
    }

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task A_site_or_person_outside_their_sites_is_not_found_not_forbidden(UserRole role)
    {
        var ids = await SeedAsync(role);
        using var client = _api.ClientAs(role);

        // Not told that it exists.
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/projects/{ids.OtherSite}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/employees/{ids.Stranger}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"/api/v1/projects/{ids.OwnSite}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"/api/v1/employees/{ids.Crew}")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task A_posting_that_has_ended_no_longer_shows_the_site(UserRole role)
    {
        var ids = await SeedAsync(role);
        var self = await EmployeeIdOfAsync(role);

        using var admin = _api.ClientAs(UserRole.SuperAdmin);
        (await admin.DeleteAsync($"/api/v1/employees/{self}/projects/{ids.OwnSite}")).EnsureSuccessStatusCode();

        using var client = _api.ClientAs(role);

        Assert.DoesNotContain(ids.OwnSite, await IdsOfAsync(client, "/api/v1/projects?pageSize=100"));
    }

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task Sees_the_work_on_their_own_sites_only(UserRole role)
    {
        var ids = await SeedAsync(role);
        using var admin = _api.ClientAs(UserRole.SuperAdmin);

        var onOwn = await CreateAsync(admin, "/api/v1/workitems",
            new { kind = "Task", title = $"Own {Guid.NewGuid():N}"[..14], projectId = ids.OwnSite, priority = "Normal" });
        var onOther = await CreateAsync(admin, "/api/v1/workitems",
            new { kind = "Task", title = $"Other {Guid.NewGuid():N}"[..14], projectId = ids.OtherSite, priority = "Normal" });

        using var client = _api.ClientAs(role);

        var visible = await IdsOfAsync(client, "/api/v1/workitems?pageSize=100");

        Assert.Contains(onOwn, visible);
        Assert.DoesNotContain(onOther, visible);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/workitems/{onOwn}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/workitems/{onOther}")).StatusCode);
    }

    // ------------------------------------------------------- fleet: vehicles and tools

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task Sees_the_fleet_on_their_own_site_and_what_is_assigned_to_them(UserRole role)
    {
        var ids = await SeedAsync(role);
        var self = await EmployeeIdOfAsync(role);
        using var admin = _api.ClientAs(UserRole.SuperAdmin);

        var onOwnSite = await CreateAsync(admin, "/api/v1/vehicles", VehicleBody());
        var onOtherSite = await CreateAsync(admin, "/api/v1/vehicles", VehicleBody());
        var toSelf = await CreateAsync(admin, "/api/v1/vehicles", VehicleBody());
        var toNobody = await CreateAsync(admin, "/api/v1/vehicles", VehicleBody());

        (await admin.PostAsync($"/api/v1/vehicles/{onOwnSite}/assign-project/{ids.OwnSite}", null)).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/v1/vehicles/{onOtherSite}/assign-project/{ids.OtherSite}", null)).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/v1/vehicles/{toSelf}/assign/{self}", null)).EnsureSuccessStatusCode();

        using var client = _api.ClientAs(role);

        var visible = await IdsOfAsync(client, "/api/v1/vehicles?pageSize=100");

        Assert.Contains(onOwnSite, visible);
        Assert.Contains(toSelf, visible);
        Assert.DoesNotContain(onOtherSite, visible);
        Assert.DoesNotContain(toNobody, visible);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/vehicles/{onOwnSite}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/vehicles/{onOtherSite}")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task Sees_the_tools_on_their_own_site_and_what_is_assigned_to_them(UserRole role)
    {
        var ids = await SeedAsync(role);
        var self = await EmployeeIdOfAsync(role);
        using var admin = _api.ClientAs(UserRole.SuperAdmin);

        var onOwnSite = await CreateAsync(admin, "/api/v1/tools", ToolBody());
        var onOtherSite = await CreateAsync(admin, "/api/v1/tools", ToolBody());
        var toSelf = await CreateAsync(admin, "/api/v1/tools", ToolBody());

        (await admin.PostAsync($"/api/v1/tools/{onOwnSite}/assign-project/{ids.OwnSite}", null)).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/v1/tools/{onOtherSite}/assign-project/{ids.OtherSite}", null)).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/v1/tools/{toSelf}/assign-employee/{self}", null)).EnsureSuccessStatusCode();

        using var client = _api.ClientAs(role);

        var visible = await IdsOfAsync(client, "/api/v1/tools?pageSize=100");

        Assert.Contains(onOwnSite, visible);
        Assert.Contains(toSelf, visible);
        Assert.DoesNotContain(onOtherSite, visible);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/tools/{onOwnSite}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/tools/{onOtherSite}")).StatusCode);
    }

    // ------------------------------------------------------------------------ schedule

    [Theory]
    [MemberData(nameof(ScopedRoles))]
    public async Task Sees_the_schedule_of_their_own_site_and_not_the_other_ones(UserRole role)
    {
        var ids = await SeedAsync(role);
        using var client = _api.ClientAs(role);

        var from = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(6).ToString("yyyy-MM-dd");

        var schedule = await client.GetFromJsonAsync<JsonElement>($"/api/v1/schedule?from={from}&to={to}");
        var rowEmployeeIds = schedule.GetProperty("rows").EnumerateArray()
            .Select(r => r.GetProperty("employeeId").GetGuid())
            .ToHashSet();

        Assert.Contains(ids.Crew, rowEmployeeIds);
        Assert.DoesNotContain(ids.Stranger, rowEmployeeIds);
    }

    private static object VehicleBody() => new
    {
        brand = "Iveco",
        model = "Daily",
        registrationNumber = $"QA-{Guid.NewGuid():N}"[..12],
        fuelType = "Diesel",
    };

    private static object ToolBody() => new
    {
        name = $"Busilica {Guid.NewGuid():N}"[..14],
    };

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.Admin)]
    public async Task Only_the_office_roles_still_see_everything(UserRole role)
    {
        var ids = await SeedAsync(UserRole.Foreman);
        using var client = _api.ClientAs(role);

        var projects = await IdsOfAsync(client, "/api/v1/projects?pageSize=100");
        var employees = await IdsOfAsync(client, "/api/v1/employees?pageSize=100");

        Assert.Contains(ids.OwnSite, projects);
        Assert.Contains(ids.OtherSite, projects);
        Assert.Contains(ids.Stranger, employees);
    }
}
