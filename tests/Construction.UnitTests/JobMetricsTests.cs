using System.Diagnostics.Metrics;
using Construction.API.Extensions;
using Construction.API.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Construction.UnitTests;

/// <summary>
/// The counters the background jobs report through.
/// </summary>
/// <remarks>
/// Worth testing for one reason: an instrument name is a string, a dashboard
/// and an alert rule refer to it by that string, and renaming one breaks both
/// silently — the query simply returns no data, which looks exactly like a
/// system with nothing to report. These tests are the thing that says the
/// names did not move.
/// </remarks>
public class JobMetricsTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly MeterListener _listener = new();
    private readonly List<(string Instrument, long Value, string? Tag)> _measurements = [];
    private readonly Lock _gate = new();

    public JobMetricsTests()
    {
        _services = new ServiceCollection().AddMetrics().BuildServiceProvider();

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == JobMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (_gate)
            {
                _measurements.Add((
                    instrument.Name,
                    value,
                    tags.Length > 0 ? tags[0].Value?.ToString() : null));
            }
        });

        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _services.Dispose();
        GC.SuppressFinalize(this);
    }

    private JobMetrics Metrics() =>
        new(_services.GetRequiredService<IMeterFactory>());

    private IReadOnlyList<(string Instrument, long Value, string? Tag)> Recorded()
    {
        lock (_gate)
        {
            return _measurements.ToList();
        }
    }

    [Fact]
    public void An_outbox_run_reports_each_outcome_under_its_own_name()
    {
        Metrics().OutboxRun(sent: 3, retried: 2, abandoned: 1);

        var recorded = Recorded();

        Assert.Contains(recorded, m => m.Instrument == "outbox.sent" && m.Value == 3);
        Assert.Contains(recorded, m => m.Instrument == "outbox.retried" && m.Value == 2);

        // The one worth alerting on: above zero means somebody definitely did
        // not get something.
        Assert.Contains(recorded, m => m.Instrument == "outbox.abandoned" && m.Value == 1);
    }

    [Fact]
    public void A_run_with_nothing_to_do_reports_nothing()
    {
        // A counter incremented by zero is a data point that says nothing and
        // still costs storage in whatever is scraping it.
        Metrics().OutboxRun(sent: 0, retried: 0, abandoned: 0);

        Assert.Empty(Recorded());
    }

    [Fact]
    public void Purges_are_tagged_by_table_so_the_growing_one_is_identifiable()
    {
        var metrics = Metrics();

        metrics.Purged("location_records", 5_000);
        metrics.Purged("refresh_tokens", 12);

        // Without the tag the total is one number and "which table" needs a
        // person with database access rather than a dashboard.
        Assert.Contains(Recorded(), m =>
            m.Instrument == "retention.purged" && m.Value == 5_000 && m.Tag == "location_records");

        Assert.Contains(Recorded(), m =>
            m.Instrument == "retention.purged" && m.Value == 12 && m.Tag == "refresh_tokens");
    }

    [Fact]
    public void A_purge_that_removed_nothing_is_not_reported()
    {
        Metrics().Purged("location_records", 0);

        Assert.Empty(Recorded());
    }

    [Fact]
    public void A_failed_job_is_counted_and_named()
    {
        Metrics().JobFailed("outbox");

        Assert.Contains(Recorded(), m =>
            m.Instrument == "job.failures" && m.Value == 1 && m.Tag == "outbox");
    }

    [Fact]
    public void Reminders_are_counted_by_kind()
    {
        Metrics().RemindersSent("document-expiry", 4);

        Assert.Contains(Recorded(), m =>
            m.Instrument == "reminders.sent" && m.Value == 4 && m.Tag == "document-expiry");
    }
}

/// <summary>
/// Whether telemetry is wired at all, and what happens when it is not.
/// </summary>
public class TelemetryExtensionsTests
{
    private static IServiceCollection Configured(string? endpoint)
    {
        var settings = new Dictionary<string, string?>();

        if (endpoint is not null)
        {
            settings[TelemetryExtensions.EndpointVariable] = endpoint;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddMetrics();
        services.AddLogging();

        return services.AddTelemetry(configuration, new TestEnvironment());
    }

    [Fact]
    public void The_job_counters_exist_even_with_nowhere_to_export_to()
    {
        // Code that has to ask whether telemetry is switched on before
        // recording anything ends up not recording it. The counters are cheap
        // when nobody is listening.
        using var services = Configured(endpoint: null).BuildServiceProvider();

        Assert.NotNull(services.GetRequiredService<JobMetrics>());
    }

    [Fact]
    public void Configuring_an_endpoint_builds_a_working_provider()
    {
        // Nothing is exported here — there is no collector at that address —
        // but a misconfigured pipeline throws when the provider is built, and
        // that is the failure this catches.
        using var services = Configured("http://localhost:4317").BuildServiceProvider();

        Assert.NotNull(services.GetRequiredService<JobMetrics>());
        Assert.NotNull(services.GetService<OpenTelemetry.Metrics.MeterProvider>());
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Construction.API";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
