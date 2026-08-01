using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Construction.API.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>Applied to the endpoints where a secret can be guessed.</summary>
    public const string CredentialsPolicy = "auth-credentials";

    /// <summary>
    /// Throttles only the endpoints that accept a guessable secret — sign-in
    /// and the password-reset pair. Verifying a password is deliberately slow,
    /// so an unthrottled attacker costs the API its CPU as well as risking the
    /// account.
    ///
    /// Deliberately NOT applied to refresh, logout or /me. A site typically
    /// reaches the API through one NAT address, so an office-wide limit would
    /// be spent on routine token refreshes and lock out real users. Refresh is
    /// already protected by its own design: the tokens are 64 random bytes and
    /// presenting a rotated one revokes every session for that account.
    ///
    /// Partitioned by client address, which is why UseForwardedHeaders has to
    /// run first — otherwise every caller shares the proxy's single partition.
    /// </summary>
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(CredentialsPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    // Sized to stop guessing without breaking a shared address:
                    // a whole office behind one NAT address, or a CI run driving
                    // several end-to-end suites, both stay comfortably inside it.
                    // Guessing a password at 20 tries a minute gets nowhere, and
                    // PBKDF2 is the real cost to an attacker either way.
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too many requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Too many attempts. Please wait a minute and try again."
                }, cancellationToken);
            };
        });

        return services;
    }
}
