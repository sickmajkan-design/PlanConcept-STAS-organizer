using Construction.Application.Features.Attachments.Commands.SendExpiryReminders;
using MediatR;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Runs the document-expiry sweep once a day.
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
public class DocumentExpiryReminderService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <summary>
    /// Delay before the first sweep, so startup is not competing with
    /// migrations and the first requests for a connection.
    /// </summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentExpiryReminderService> _logger;

    public DocumentExpiryReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentExpiryReminderService> logger)
    {
        _scopeFactory = scopeFactory;
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
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            var sent = await mediator.Send(
                new SendExpiryRemindersCommand(), cancellationToken);

            if (sent > 0)
            {
                _logger.LogInformation(
                    "Sent expiry reminders for {Count} document(s).", sent);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A failed sweep must not take the host down with it — the
            // documents are still there and tomorrow's run will find them,
            // because nothing was marked as reminded.
            _logger.LogError(
                exception, "The document expiry sweep failed; it will run again.");
        }
    }
}
