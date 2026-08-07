using System.Diagnostics;
using Construction.API.Observability;
using Construction.Application.Features.Outbox.Commands.ProcessOutbox;
using MediatR;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Sends what the request path queued.
/// </summary>
/// <remarks>
/// <para>
/// Every ten seconds, because this is the difference between a password-reset
/// email arriving in seconds and arriving whenever the next sweep happens to
/// run. A pass with nothing due is one indexed query against a filtered index
/// that is empty when there is no backlog.
/// </para>
/// <para>
/// Safe on every replica. Claiming is a single <c>UPDATE</c> that stamps a
/// token and pushes the message beyond its lease, so two workers cannot take
/// the same message — PostgreSQL locks the row for the update and the loser
/// re-checks its predicate afterwards, by which point the message is no longer
/// due.
/// </para>
/// </remarks>
public class OutboxService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Short, unlike the other two timers. The queue holds things somebody is
    /// waiting for, so the first pass should not be minutes away.
    /// </summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JobMetrics _metrics;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(
        IServiceScopeFactory scopeFactory,
        JobMetrics metrics,
        ILogger<OutboxService> logger)
    {
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using var timer = new PeriodicTimer(Interval);

            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown, not a failure. Anything claimed comes back by itself
            // when its lease expires.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            var result = await mediator.Send(new ProcessOutboxCommand(), cancellationToken);

            _metrics.OutboxRun(result.Sent, result.Retrying, result.Abandoned);

            // Failures are already logged per message, with the reason. This
            // line is for the shape of a run, and only when there was one —
            // six lines a minute saying "nothing to do" is how a log stops
            // being read.
            if (result.Handled > 0)
            {
                _logger.LogInformation(
                    "Outbox: {Sent} sent, {Retrying} retrying, {Abandoned} abandoned.",
                    result.Sent,
                    result.Retrying,
                    result.Abandoned);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A failed pass must not take the host down. Claimed messages
            // return when their lease expires; unclaimed ones were never
            // touched.
            _metrics.JobFailed("outbox");

            _logger.LogError(exception, "The outbox pass failed; it will run again.");
        }
        finally
        {
            _metrics.JobFinished("outbox", Stopwatch.GetElapsedTime(started));
        }
    }
}
