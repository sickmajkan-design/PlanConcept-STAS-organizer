using System.Diagnostics.Metrics;

namespace Construction.API.Observability;

/// <summary>
/// What the background jobs did, as numbers something can alert on.
/// </summary>
/// <remarks>
/// <para>
/// Request metrics say the API is up. These say the work is getting done,
/// which is not the same thing and fails independently: an outbox that cannot
/// reach the mail server still serves 200s all day, and the only visible
/// symptom is that nobody receives a password-reset email — reported, if at
/// all, by a user, days later.
/// </para>
/// <para>
/// The two worth an alert are <c>outbox.abandoned</c> above zero, which means
/// somebody definitely did not get something, and <c>job.failures</c>, which
/// means a sweep is not running at all.
/// </para>
/// </remarks>
public sealed class JobMetrics
{
    public const string MeterName = "Construction.Jobs";

    private readonly Counter<long> _outboxSent;
    private readonly Counter<long> _outboxRetried;
    private readonly Counter<long> _outboxAbandoned;
    private readonly Counter<long> _purged;
    private readonly Counter<long> _remindersSent;
    private readonly Counter<long> _jobFailures;
    private readonly Histogram<double> _jobDuration;

    public JobMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _outboxSent = meter.CreateCounter<long>(
            "outbox.sent", "{message}", "Messages delivered.");

        _outboxRetried = meter.CreateCounter<long>(
            "outbox.retried", "{message}", "Delivery attempts that failed and will be retried.");

        _outboxAbandoned = meter.CreateCounter<long>(
            "outbox.abandoned", "{message}", "Messages given up on. Somebody did not get something.");

        _purged = meter.CreateCounter<long>(
            "retention.purged", "{row}", "Rows deleted by the retention sweep.");

        _remindersSent = meter.CreateCounter<long>(
            "reminders.sent", "{notification}", "Expiry and deadline reminders sent.");

        _jobFailures = meter.CreateCounter<long>(
            "job.failures", "{failure}", "Background job passes that threw.");

        _jobDuration = meter.CreateHistogram<double>(
            "job.duration", "ms", "How long a background job pass took.");
    }

    public void OutboxRun(int sent, int retried, int abandoned)
    {
        Add(_outboxSent, sent);
        Add(_outboxRetried, retried);
        Add(_outboxAbandoned, abandoned);
    }

    /// <summary>Tagged by table, so "which one is growing" is answerable.</summary>
    public void Purged(string table, int rows)
    {
        if (rows > 0)
        {
            _purged.Add(rows, new KeyValuePair<string, object?>("table", table));
        }
    }

    public void RemindersSent(string kind, int count)
    {
        if (count > 0)
        {
            _remindersSent.Add(count, new KeyValuePair<string, object?>("kind", kind));
        }
    }

    public void JobFailed(string job) =>
        _jobFailures.Add(1, new KeyValuePair<string, object?>("job", job));

    public void JobFinished(string job, TimeSpan elapsed) =>
        _jobDuration.Record(
            elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("job", job));

    private static void Add(Counter<long> counter, int value)
    {
        if (value > 0)
        {
            counter.Add(value);
        }
    }
}
