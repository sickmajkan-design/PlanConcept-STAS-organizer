using System.Diagnostics;
using Construction.API.Observability;
using Construction.Application.Features.Maintenance.Commands.PurgeOrphanedNotifications;
using MediatR;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Sweeps away notifications whose referenced record no longer exists.
/// </summary>
/// <remarks>
/// <para>
/// A notification stores what it is about as a plain id inside a JSON blob,
/// not a foreign key (see the doc comment on
/// <see cref="PurgeOrphanedNotificationsCommand"/> for why), so nothing in
/// the database can cascade a delete into this table. Deleting the absence,
/// document, or work item a notification points at — through the app, or
/// directly in the database — leaves the notification exactly as visible as
/// before, now pointing at nothing. This sweep is what actually notices.
/// </para>
/// <para>
/// Two hours rather than daily: the table this reads is small (one row per
/// notification sent, not one per GPS ping), so a frequent, cheap pass costs
/// nothing, and a stale notification sitting in someone's inbox for the rest
/// of the day is exactly the complaint this exists to fix.
/// </para>
/// <para>
/// Safe to run on every replica. Each pass computes its own orphan set from
/// the database as it is right now and deletes exactly that set; two
/// replicas running at once either find disjoint orphans or one finds the
/// rows the other already removed, and a row that no longer exists simply
/// is not deleted twice.
/// </para>
/// </remarks>
public class NotificationReconciliationService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(2);

    /// <summary>
    /// Delay before the first sweep, so startup is not competing with
    /// migrations and the first requests for a connection.
    /// </summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JobMetrics _metrics;
    private readonly ILogger<NotificationReconciliationService> _logger;

    public NotificationReconciliationService(
        IServiceScopeFactory scopeFactory,
        JobMetrics metrics,
        ILogger<NotificationReconciliationService> logger)
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
            // Shutdown, not a failure.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            var removed = await mediator.Send(
                new PurgeOrphanedNotificationsCommand(), cancellationToken);

            _metrics.Purged("orphaned_notifications", removed);

            // Only when it did something — a line every two hours saying
            // "removed nothing" is noise that trains people to skip the ones
            // that matter.
            if (removed > 0)
            {
                _logger.LogInformation(
                    "Notification reconciliation removed {Count} notification(s) "
                    + "referencing a record that no longer exists.",
                    removed);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A failed sweep must not take the host down. Nothing was half
            // done — the delete is one statement — so the next run simply
            // finds the same orphans still there.
            _metrics.JobFailed("notification_reconciliation");

            _logger.LogError(
                exception, "The notification reconciliation sweep failed; it will run again.");
        }
        finally
        {
            _metrics.JobFinished(
                "notification_reconciliation", Stopwatch.GetElapsedTime(started));
        }
    }
}
