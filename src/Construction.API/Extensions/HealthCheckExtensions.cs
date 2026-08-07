using System.Text.Json;
using Construction.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Construction.API.Extensions;

/// <summary>
/// Two probes that answer two different questions.
/// </summary>
/// <remarks>
/// <para>
/// They were one endpoint, and it checked the database. That conflates "this
/// process is broken, restart it" with "this instance cannot serve traffic
/// right now" — and an orchestrator acts on the first by killing the
/// container. A database failover of thirty seconds would therefore restart
/// every replica, several times, during the one incident when losing them all
/// is least affordable. Restarting an API because PostgreSQL is briefly away
/// fixes nothing and costs the warm connection pools.
/// </para>
/// <para>
/// So: <c>/health/live</c> answers only whether the process is running and can
/// still serve a request. It has no checks in it deliberately — a liveness
/// probe that depends on anything external is a way of asking a third party
/// for permission to stay alive. <c>/health/ready</c> checks the database,
/// because an instance that cannot reach it should be taken out of the load
/// balancer rather than killed.
/// </para>
/// </remarks>
public static class HealthCheckExtensions
{
    /// <summary>Checks that decide whether this instance can serve traffic.</summary>
    public const string ReadyTag = "ready";

    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: [ReadyTag]);

        return services;
    }

    public static void MapApplicationHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            // No checks at all. If this request is answered, the process is
            // alive; that is the entire question.
            Predicate = _ => false,
            ResponseWriter = WriteAsync,
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteAsync,
        });

        // Kept because things already point at it — the README, and anything
        // somebody has already configured. Readiness is the more useful of the
        // two answers to give a caller who did not choose.
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteAsync,
        });
    }

    /// <summary>
    /// Reports which check failed, and nothing about why.
    /// </summary>
    /// <remarks>
    /// The name and the duration are enough to send somebody to the right
    /// place. The exception is not included on purpose: these endpoints are
    /// unauthenticated, and a failed database check carries an Npgsql message
    /// naming the host, the database and the user it tried to connect as.
    /// That belongs in the log, where it already is.
    /// </remarks>
    private static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        // A cached health response is a probe answering about the past.
        context.Response.Headers.CacheControl = "no-store, no-cache";

        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
            }),
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
