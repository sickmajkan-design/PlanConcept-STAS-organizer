using System.Net;
using System.Text.Json;

namespace Construction.IntegrationTests;

/// <summary>
/// The two probes, and the difference between them.
/// </summary>
/// <remarks>
/// The one that matters is that liveness runs no checks. It cannot be
/// demonstrated by reading the code — a predicate returning false looks like
/// an oversight — so it is asserted here: no check names come back, and the
/// endpoint answers even when the readiness check would have work to do.
/// </remarks>
[Collection(ApiCollection.Name)]
public class HealthCheckTests
{
    private readonly ApiFixture _api;

    public HealthCheckTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<JsonElement> GetAsync(string path, HttpStatusCode expected)
    {
        using var client = _api.AnonymousClient();

        var response = await client.GetAsync(path);

        Assert.Equal(expected, response.StatusCode);

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact]
    public async Task Liveness_answers_without_running_a_single_check()
    {
        // The whole point. A liveness probe that depends on anything external
        // is a way of asking a third party for permission to stay alive: a
        // thirty-second database failover would otherwise restart every
        // replica, several times, during the one incident when losing them all
        // is least affordable.
        var body = await GetAsync("/health/live", HttpStatusCode.OK);

        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.Empty(body.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task Readiness_checks_the_database_and_says_which_check_it_ran()
    {
        var body = await GetAsync("/health/ready", HttpStatusCode.OK);

        Assert.Equal("Healthy", body.GetProperty("status").GetString());

        var checks = body.GetProperty("checks").EnumerateArray().ToList();

        var database = Assert.Single(checks, check =>
            check.GetProperty("name").GetString() == "database");

        Assert.Equal("Healthy", database.GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_old_path_still_answers()
    {
        // Things already point at it — the README, and whatever anybody has
        // already configured. Moving it would be a breaking change for a
        // deployment that did nothing wrong.
        var body = await GetAsync("/health", HttpStatusCode.OK);

        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Neither_probe_leaks_anything_about_the_connection()
    {
        // These endpoints are unauthenticated. A failed database check carries
        // an Npgsql message naming the host, the database and the user it
        // tried to connect as; that belongs in the log, not in a response
        // anybody can fetch.
        using var client = _api.AnonymousClient();

        foreach (var path in new[] { "/health", "/health/live", "/health/ready" })
        {
            var text = await client.GetStringAsync(path);

            Assert.DoesNotContain("Host=", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Username", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("exception", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task A_probe_response_is_never_cached()
    {
        // A cached health response is a probe answering about the past, which
        // is the one thing a probe must not do.
        using var client = _api.AnonymousClient();

        var response = await client.GetAsync("/health/ready");

        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task Both_probes_are_open_to_an_unauthenticated_caller()
    {
        // An orchestrator has no credentials, and a probe that needed them
        // would report the API unhealthy for the wrong reason.
        using var client = _api.AnonymousClient();

        foreach (var path in new[] { "/health/live", "/health/ready" })
        {
            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
