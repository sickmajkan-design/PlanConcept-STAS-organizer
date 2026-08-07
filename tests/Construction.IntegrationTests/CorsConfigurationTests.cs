using Construction.API.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Construction.IntegrationTests;

/// <summary>
/// That the CORS validation is wired into startup, and that a valid origin
/// still reaches the browser.
/// </summary>
/// <remarks>
/// <para>
/// <c>CorsOriginsTests</c> proves the rule and the wording of its message.
/// This proves the rule is applied: a validator nobody calls is worth nothing,
/// and deleting the one line in <c>Program.cs</c> that calls it would leave
/// every unit test green.
/// </para>
/// <para>
/// Overrides go through <c>UseSetting</c> rather than
/// <c>ConfigureAppConfiguration</c>, which is too late — the origins are read
/// off <c>builder.Configuration</c> while services are being registered, and
/// the deferred configuration callbacks do not run until <c>Build()</c>.
/// </para>
/// </remarks>
[Collection(StandaloneHostCollection.Name)]
public class CorsConfigurationTests
{
    /// <summary>
    /// Hosts the API with <paramref name="origins"/> written over the front of
    /// the configured list.
    /// </summary>
    /// <remarks>
    /// Written over, not replacing: configuration arrays merge by index, so
    /// the two localhost entries in appsettings.Development.json are still
    /// there behind whatever is passed here. That is harmless for every
    /// assertion below — an extra allowed origin changes nothing about whether
    /// this one is allowed, or about whether startup survives a malformed one.
    /// </remarks>
    private static WebApplicationFactory<Program> Host(params string[] origins)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            // Nothing here touches the database; the app must fail, or not,
            // while reading configuration and long before a query.
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=127.0.0.1;Port=1;Database=nothing;Username=nobody;Password=none;Timeout=1");

            builder.UseSetting("JwtSettings:Issuer", "construction-api-tests");
            builder.UseSetting("JwtSettings:Audience", "construction-clients-tests");
            builder.UseSetting(
                "JwtSettings:SecretKey", "integration-test-signing-key-at-least-32-chars");
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");

            for (var index = 0; index < origins.Length; index++)
            {
                builder.UseSetting($"Cors:AllowedOrigins:{index}", origins[index]);
            }

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

                services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Critical));
            });
        });
    }

    private static async Task<HttpResponseMessage> PreflightAsync(
        HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/employees");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task An_origin_with_a_trailing_slash_stops_the_application_starting()
    {
        // The whole point of the change. Left alone this deploys cleanly and
        // the admin panel cannot talk to it, with nothing in the server log to
        // say why.
        //
        // The assertion is that it does not start, not what it says while not
        // starting: Program.cs catches at the top level to log the failure and
        // set a non-zero exit code, so the host builder can only report that no
        // host was ever built. The message is pinned by CorsOriginsTests. What
        // ties the two together is the test below — the same configuration with
        // one character removed from the origin, and it starts.
        await using var factory = Host("https://admin.example.com/");

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task A_well_formed_origin_starts_and_is_allowed_through()
    {
        await using var factory = Host("https://admin.example.com");

        using var client = factory.CreateClient();

        var response = await PreflightAsync(client, "https://admin.example.com");

        Assert.Equal(
            "https://admin.example.com",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task The_download_file_name_is_exposed_to_the_panel()
    {
        // Response headers are hidden from cross-origin JavaScript unless the
        // policy names them, so without this every export arrives called
        // whatever the client guessed. It rides on an ordinary request rather
        // than a preflight — Access-Control-Expose-Headers is not part of a
        // preflight answer, which is why this is a separate test.
        await using var factory = Host("https://admin.example.com");

        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "https://admin.example.com");

        var response = await client.SendAsync(request);

        Assert.Contains(
            "Content-Disposition",
            response.Headers.GetValues("Access-Control-Expose-Headers"));
    }

    [Fact]
    public async Task An_origin_that_was_never_configured_is_refused()
    {
        await using var factory = Host("https://admin.example.com");

        using var client = factory.CreateClient();

        var response = await PreflightAsync(client, "https://attacker.example.com");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task The_shipped_development_origins_work_untouched()
    {
        // A fresh clone runs `npm run dev` on 5173 and expects to reach the
        // API. This is the one configuration nobody edits before using, so a
        // typo in it would be found by every new contributor and nobody else.
        await using var factory = Host();

        using var client = factory.CreateClient();

        var response = await PreflightAsync(client, "http://localhost:5173");

        Assert.Equal(
            "http://localhost:5173",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }
}
