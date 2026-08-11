using System.Net;
using System.Net.Http.Json;
using Construction.API.BackgroundServices;
using Construction.API.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Construction.IntegrationTests;

/// <summary>
/// A client stuck in a crash loop is cut off.
/// </summary>
/// <remarks>
/// <para>
/// A render loop reports as fast as the network allows, and every report after
/// the first carries the same stack. Both clients throttle themselves; this is
/// the half that does not depend on the client being the version that does.
/// </para>
/// <para>
/// On its own host, and therefore in <see cref="StandaloneHostCollection"/>,
/// because exhausting a limiter is not something to do to a shared one: the
/// other client-error tests would then pass or fail on the order they happened
/// to run in.
/// </para>
/// </remarks>
[Collection(StandaloneHostCollection.Name)]
public class ClientErrorFloodTests
{
    private static WebApplicationFactory<Program> Host()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=1;Database=nothing;Username=nobody;Password=none;Timeout=1");

            builder.UseSetting("JwtSettings:Issuer", "construction-api-tests");
            builder.UseSetting("JwtSettings:Audience", "construction-clients-tests");
            builder.UseSetting(
                "JwtSettings:SecretKey", "integration-test-signing-key-at-least-32-chars");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");

            builder.ConfigureServices(services =>
            {
                foreach (var timer in services
                    .Where(descriptor =>
                        descriptor.ImplementationType == typeof(DailyReminderService) ||
                        descriptor.ImplementationType == typeof(DataRetentionService) ||
                        descriptor.ImplementationType == typeof(OutboxService))
                    .ToList())
                {
                    services.Remove(timer);
                }
            });
        });
    }

    [Fact]
    public async Task A_flood_of_reports_is_throttled()
    {
        using var host = Host();
        using var client = host.CreateClient();

        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < RateLimitingExtensions.ClientErrorsPerMinute + 5; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/client-errors",
                new { app = "admin", message = "the same fault, again" });

            statuses.Add(response.StatusCode);
        }

        // The endpoint touches no database, so the ones that get through are
        // accepted rather than failing on the unreachable connection string.
        Assert.Equal(
            RateLimitingExtensions.ClientErrorsPerMinute,
            statuses.Count(status => status == HttpStatusCode.Accepted));

        Assert.Equal(5, statuses.Count(status => status == HttpStatusCode.TooManyRequests));
    }
}
