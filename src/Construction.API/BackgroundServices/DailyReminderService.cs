using System.Diagnostics;
using Construction.API.Observability;
using Construction.Application.Features.Attachments.Commands.SendExpiryReminders;
using Construction.Application.Features.WorkItems.Commands.SendDueReminders;
using MediatR;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Runs the daily reminder sweeps: documents about to lapse, and work about to
/// fall due.
/// </summary>
/// <remarks>
/// A hosted service rather than a scheduling library. The product has exactly
/// one recurring job, and a job framework brings a database schema, a
/// dashboard and an operational surface to run it — all of which would be more
/// to maintain than the job itself.
///
/// Two instances of the API both run this, which is fine:
/// <see cref="SendExpiryRemindersCommand"/> claims each document with a
/// conditional update before notifying, so a duplicate run finds nothing left
/// to claim rather than telling anyone twice.
/// </remarks>
public class DailyReminderService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <summary>
    /// Delay before the first sweep, so startup is not competing with
    /// migrations and the first requests for a connection.
    /// </summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JobMetrics _metrics;
    private readonly ILogger<DailyReminderService> _logger;

    public DailyReminderService(
        IServiceScopeFactory scopeFactory,
        JobMetrics metrics,
        ILogger<DailyReminderService> logger)
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

            var documents = await mediator.Send(
                new SendExpiryRemindersCommand(), cancellationToken);

            _metrics.RemindersSent("document-expiry", documents);

            if (documents > 0)
            {
                _logger.LogInformation(
                    "Sent expiry reminders for {Count} document(s).", documents);
            }

            var work = await mediator.Send(
                new SendDueRemindersCommand(), cancellationToken);

            _metrics.RemindersSent("work-item-due", work);

            if (work > 0)
            {
                _logger.LogInformation(
                    "Sent deadline reminders for {Count} work item(s).", work);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A failed sweep must not take the host down with it — the rows
            // are still there and tomorrow's run will find them, because
            // nothing that was not sent got marked as sent.
            _metrics.JobFailed("reminders");

            _logger.LogError(
                exception, "The daily reminder sweep failed; it will run again.");
        }
        finally
        {
            _metrics.JobFinished("reminders", Stopwatch.GetElapsedTime(started));
        }
    }
}
