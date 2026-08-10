using System.Net;
using System.Net.Http.Json;
using Construction.API.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Construction.IntegrationTests;

/// <summary>
/// The credential rate limit is actually configurable, and actually applied.
/// </summary>
/// <remarks>
/// <para>
/// The limit used to be a constant 20 a minute.
/// <c>scripts/loadtest-login.sh --one-address</c> showed what that costs a site
/// where everybody shares one connection: of sixty sign-ins, twenty succeeded
/// and forty were refused — with the right password. A crew arriving at shift
/// change through one router is exactly that shape.
/// </para>
/// <para>
/// So the number moved into configuration, and that turns a constant into a
/// promise: the setting is read, and the value in it is the value enforced.
/// A promise nothing checks is how a security control ends up quietly off, so
/// this sets a deliberately tiny limit and counts.
/// </para>
/// <para>
/// No database is involved, and that is not only tidiness. The first version
/// sent a well-formed sign-in, which reaches the handler and waits on a
/// connection to a database that is not there. Alone it passed; in the full
/// suite it failed intermittently with 500 where it expected 429 — under load
/// each request sat on connection timeouts long enough for the sixty-second
/// window to roll over mid-test, refilling the very allowance being counted.
/// An empty address fails validation before the handler runs, so every request
/// is immediate and the window cannot move underneath it. The window is also
/// set far longer than any plausible run.
/// </para>
/// </remarks>
public class AuthRateLimitConfigurationTests
{
    private const int Limit = 3;

    /// <summary>Fails validation, so it never reaches the handler or a database.</summary>
    private static object NotEvenAnAddress() => new { email = "", password = "" };

    private static WebApplicationFactory<Program> Host(int permitLimit)
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

            builder.UseSetting("Auth:RateLimit:PermitLimit", permitLimit.ToString());
            // Far longer than any run. A fixed window that rolls mid-test
            // refills the allowance being counted, which is exactly how the
            // first version of this file failed under load.
            builder.UseSetting("Auth:RateLimit:WindowSeconds", "600");

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
    public async Task The_configured_limit_is_the_limit_enforced()
    {
        using var host = Host(Limit);
        using var client = host.CreateClient();

        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < Limit + 2; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", NotEvenAnAddress());

            statuses.Add(response.StatusCode);
        }

        // The first `Limit` get past the limiter and are then rejected by
        // validation. What matters here is only that the limiter let them by.
        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statuses.Take(Limit));

        // And everything after is refused, without the handler being reached.
        Assert.All(
            statuses.Skip(Limit),
            status => Assert.Equal(HttpStatusCode.TooManyRequests, status));
    }

    /// <summary>
    /// The refusal explains itself without blaming the reader.
    /// </summary>
    /// <remarks>
    /// It used to say "Too many attempts. Please wait a minute and try again."
    /// The limit counts every attempt from an address, not only failed ones, so
    /// on a shared connection that sentence arrives on a correct password and
    /// sends somebody hunting for a mistake they did not make.
    /// </remarks>
    [Fact]
    public async Task The_refusal_mentions_the_shared_connection()
    {
        using var host = Host(1);
        using var client = host.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/login", NotEvenAnAddress());

        var refused = await client.PostAsJsonAsync("/api/v1/auth/login", NotEvenAnAddress());

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);

        var body = await refused.Content.ReadAsStringAsync();

        Assert.Contains("share", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Too many attempts.", body, StringComparison.Ordinal);
    }
}
