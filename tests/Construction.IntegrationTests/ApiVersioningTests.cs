using System.Net;
using System.Net.Http.Json;
using Construction.API.Extensions;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Both shapes of every route, and the promise that ties them together.
/// </summary>
/// <remarks>
/// Versioning is only worth anything if the old paths keep meaning what they
/// meant. These assert both halves: <c>/api/v1/…</c> answers, and
/// <c>/api/…</c> still answers the same way.
/// </remarks>
[Collection(ApiCollection.Name)]
public class ApiVersioningTests
{
    private readonly ApiFixture _api;

    public ApiVersioningTests(ApiFixture api)
    {
        _api = api;
    }

    [Theory]
    [InlineData("employees")]
    [InlineData("projects")]
    [InlineData("vehicles")]
    [InlineData("tools")]
    [InlineData("materials")]
    [InlineData("users")]
    [InlineData("workitems")]
    [InlineData("timeentries")]
    [InlineData("absences")]
    [InlineData("notifications")]
    [InlineData("attachments")]
    public async Task A_versioned_path_reaches_the_same_place_as_the_old_one(string resource)
    {
        using var versioned = _api.ClientAs(UserRole.SuperAdmin);
        using var legacy = _api.ClientAs(UserRole.SuperAdmin);

        var withVersion = await versioned.GetAsync($"/api/v1/{resource}");
        var without = await legacy.GetAsync($"/api/{resource}");

        Assert.Equal(without.StatusCode, withVersion.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotFound, withVersion.StatusCode);
    }

    [Theory]
    // The routes declared absolutely rather than off the controller template.
    // They needed a second attribute each, which is exactly the kind of thing
    // that gets missed one at a time.
    [InlineData("schedule?from=2026-08-03&to=2026-08-09")]
    [InlineData("employee-rates")]
    [InlineData("material-movements")]
    [InlineData("vehicle-expenses")]
    [InlineData("costs/projects?from=2026-08-01&to=2026-08-31")]
    [InlineData("costs/vehicles?from=2026-08-01&to=2026-08-31")]
    public async Task The_hand_written_routes_answer_on_both_forms(string path)
    {
        using var versioned = _api.ClientAs(UserRole.SuperAdmin);
        using var legacy = _api.ClientAs(UserRole.SuperAdmin);

        var withVersion = await versioned.GetAsync($"/api/v1/{path}");
        var without = await legacy.GetAsync($"/api/{path}");

        Assert.Equal(without.StatusCode, withVersion.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotFound, withVersion.StatusCode);
    }

    [Fact]
    public async Task Signing_in_works_on_the_versioned_path()
    {
        // Auth is the one a client reaches first, and the one whose failure
        // looks like wrong credentials rather than a missing route.
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_server_says_which_versions_it_speaks()
    {
        // So a client can find out without reading documentation for a
        // deployment it may not be looking at.
        using var client = _api.ClientAs(UserRole.Admin);

        var response = await client.GetAsync("/api/v1/employees");

        Assert.True(response.Headers.TryGetValues("api-supported-versions", out var values));
        Assert.Contains("1.0", values!);
    }

    [Fact]
    public async Task A_version_that_does_not_exist_is_not_quietly_served_as_version_one()
    {
        // The property that matters is the absence of a silent fallback. A
        // client asking for v9 has been written against a contract this server
        // does not have; answering it with v1 data would look like success and
        // be wrong in whatever way v9 was supposed to differ.
        //
        // The status is 404 rather than the library's usual 400 because
        // nothing claims that route at all — no controller declares 9.0, so
        // routing finds no candidate before version negotiation is reached.
        // Either answer is a clear failure; being served v1 would not be.
        using var client = _api.ClientAs(UserRole.Admin);

        var response = await client.GetAsync("/api/v9/employees");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void An_unversioned_request_means_version_one_and_must_always_mean_version_one()
    {
        // Not a behaviour test — a guard on a decision. Bumping the default
        // would silently move every client that has not been updated onto a
        // version it was never written for, arriving as changed behaviour
        // rather than as an error. That is the exact failure versioning exists
        // to prevent, so it should be hard to do by accident.
        Assert.Equal(1, ApiVersioningExtensions.Default.MajorVersion);
        Assert.Equal(0, ApiVersioningExtensions.Default.MinorVersion);
    }

    [Fact]
    public async Task The_health_probes_are_not_versioned()
    {
        // An orchestrator's probe URL should not have to change when the API
        // does, and a probe is not part of the contract a client codes against.
        using var client = _api.AnonymousClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/health/live")).StatusCode);
    }
}
