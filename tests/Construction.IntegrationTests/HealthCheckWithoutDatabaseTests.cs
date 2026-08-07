using System.Net;
using System.Text.Json;
using Construction.API.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Construction.IntegrationTests;

/// <summary>
/// What the two probes say when the database is gone.
/// </summary>
/// <remarks>
/// <para>
/// The reason the split exists, and the one thing that cannot be shown by
/// reading the configuration. The API is hosted against a connection string
/// pointing at a port nobody is listening on — no server to stop, nothing
/// shared with the other tests, and the failure is the real one rather than a
/// stubbed health check reporting what it was told to.
/// </para>
/// <para>
/// Liveness must stay green. If it went red here, an orchestrator would kill
/// every replica during a database failover — which fixes nothing, loses the
/// warm connection pools, and turns thirty seconds of degradation into a
/// restart loop.
/// </para>
/// </remarks>
[Collection(StandaloneHostCollection.Name)]
public class HealthCheckWithoutDatabaseTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                // Port 1 on the loopback: reserved, never listened on, and it
                // refuses immediately rather than hanging for a timeout.
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    "Host=127.0.0.1;Port=1;Database=nothing;Username=nobody;Password=none;"
                    + "Timeout=1;Command Timeout=1");

                builder.UseSetting("JwtSettings:Issuer", "construction-api-tests");
                builder.UseSetting("JwtSettings:Audience", "construction-clients-tests");
                builder.UseSetting(
                    "JwtSettings:SecretKey", "integration-test-signing-key-at-least-32-chars");
                builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");

                builder.ConfigureServices(services =>
                {
                    // They would spend the test retrying a database that is
                    // not there and fill the output with it.
                    foreach (var timer in services
                        .Where(descriptor =>
                            descriptor.ImplementationType == typeof(DailyReminderService) ||
                            descriptor.ImplementationType == typeof(DataRetentionService) ||
                            descriptor.ImplementationType == typeof(OutboxService))
                        .ToList())
                    {
                        services.Remove(timer);
                    }

                    services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Critical));
                });
            });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Liveness_stays_green_when_the_database_is_unreachable()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_goes_red_when_the_database_is_unreachable()
    {
        // The instance should come out of the load balancer, which is a
        // different remedy from being killed.
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("Unhealthy", body.GetProperty("status").GetString());

        var database = Assert.Single(
            body.GetProperty("checks").EnumerateArray(),
            check => check.GetProperty("name").GetString() == "database");

        Assert.Equal("Unhealthy", database.GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_failing_readiness_check_still_says_nothing_about_the_connection()
    {
        // The case the redaction is actually for. A live Npgsql failure names
        // the host, the port, the database and the user it tried to connect
        // as — on an endpoint anybody can fetch.
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var text = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("127.0.0.1", text);
        Assert.DoesNotContain("nobody", text);
        Assert.DoesNotContain("nothing", text);
        Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
    }
}
