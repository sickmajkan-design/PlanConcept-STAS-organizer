using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Construction.API.Extensions;

/// <summary>How many credential attempts one address gets, and over what window.</summary>
/// <remarks>
/// <para>
/// Configurable because the right number depends on how the people using it
/// reach the API, and that is not knowable from here. It was a constant 20, and
/// <c>scripts/loadtest-login.sh --one-address</c> showed what that means for a
/// crew sharing one address: 20 sign in, the rest are refused, and the refusal
/// reads like a wrong password to somebody who typed the right one.
/// </para>
/// <para>
/// The default is now 120 a minute. The reasoning, from the same measurements:
/// the CPU ceiling on four cores is about 80 sign-ins a second — near 5,000 a
/// minute — so 120 is nowhere near a denial-of-service risk, and it covers a
/// hundred-person shift change arriving through one router. Against guessing it
/// gives up little that matters at this size: an installation has hundreds of
/// accounts, not millions, so a sprayer at either 20 or 120 a minute walks the
/// whole list quickly, and what actually stops them is password strength and
/// the per-account lockout after ten failures.
/// </para>
/// <para>
/// A deployment where every client has its own address can safely lower it.
/// </para>
/// </remarks>
public class AuthRateLimitSettings
{
    public const string SectionName = "Auth:RateLimit";

    public int PermitLimit { get; set; } = 120;

    public int WindowSeconds { get; set; } = 60;
}

public static class RateLimitingExtensions
{
    /// <summary>Applied to the endpoints where a secret can be guessed.</summary>
    public const string CredentialsPolicy = "auth-credentials";

    /// <summary>Applied to the unauthenticated client-error endpoint.</summary>
    public const string ClientErrorPolicy = "client-errors";

    /// <summary>
    /// Reports allowed from one address per minute.
    /// </summary>
    /// <remarks>
    /// Generous enough for a genuinely broken screen — a render loop can throw
    /// a dozen times before anybody's finger leaves the glass — and small
    /// enough that a client stuck in a crash loop cannot write the disk full.
    /// The reports that follow the first few say nothing new anyway: it is the
    /// same stack.
    /// </remarks>
    public const int ClientErrorsPerMinute = 30;

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
    public static IServiceCollection AddAuthRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(AuthRateLimitSettings.SectionName)
            .Get<AuthRateLimitSettings>() ?? new AuthRateLimitSettings();

        var permitLimit = settings.PermitLimit > 0
            ? settings.PermitLimit
            : new AuthRateLimitSettings().PermitLimit;

        var window = settings.WindowSeconds > 0
            ? TimeSpan.FromSeconds(settings.WindowSeconds)
            : TimeSpan.FromMinutes(1);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(CredentialsPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0
                }));

            options.AddPolicy(ClientErrorPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = ClientErrorsPerMinute,
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
                    // Says what happened rather than blaming the reader. The
                    // limit counts every attempt from an address, so on a site
                    // where everyone shares one connection this can arrive on a
                    // perfectly correct password — and being told "too many
                    // attempts" then sends people hunting for a mistake they
                    // did not make.
                    detail = "This connection has made too many sign-in attempts in a short "
                        + "time. If several people share it, wait a minute and try again."
                }, cancellationToken);
            };
        });

        return services;
    }
}
