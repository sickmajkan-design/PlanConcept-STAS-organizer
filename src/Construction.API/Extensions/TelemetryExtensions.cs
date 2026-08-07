using Construction.API.Observability;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Construction.API.Extensions;

/// <summary>
/// Metrics and traces, exported over OTLP when somewhere is configured to send
/// them.
/// </summary>
/// <remarks>
/// <para>
/// OTLP rather than a vendor SDK, and off unless
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set. Every aggregator worth using
/// speaks it — Prometheus through a collector, Grafana, Jaeger, and the hosted
/// ones — so the choice of backend stays a deployment decision instead of a
/// dependency in this file. A developer running the API locally gets nothing
/// extra and pays nothing.
/// </para>
/// <para>
/// What this cannot do from inside the repository: run the aggregator, build
/// the dashboards, or write the alert rules. Those are the half of
/// observability that lives in the deployment, and shipping the signal is what
/// makes them possible rather than what replaces them.
/// </para>
/// </remarks>
public static class TelemetryExtensions
{
    /// <summary>The standard variable; setting it is what turns this on.</summary>
    public const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Registered whether or not anything is exported: the counters are
        // cheap when nobody is listening, and code that has to ask whether
        // telemetry is on before recording anything ends up not recording it.
        services.AddSingleton<JobMetrics>();

        var endpoint = configuration[EndpointVariable];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return services;
        }

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "construction-api",
                    serviceVersion: typeof(TelemetryExtensions).Assembly.GetName().Version?.ToString())
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment", environment.EnvironmentName),
                ]))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                // The product's own numbers. Request rates say the API is up;
                // these say the work is actually getting done — an outbox that
                // is failing every message still serves 200s.
                .AddMeter(JobMetrics.MeterName)
                .AddOtlpExporter())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Health checks are polled every few seconds by the
                    // orchestrator and tell nobody anything.
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        return services;
    }
}
