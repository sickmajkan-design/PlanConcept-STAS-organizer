using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Writes "last seen" from the in-memory tracker to each account's latest
/// session, every few minutes: one small update per active person per run,
/// instead of one per request.
/// </summary>
public class PresenceFlushService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPresenceTracker _tracker;
    private readonly ILogger<PresenceFlushService> _logger;

    public PresenceFlushService(
        IServiceScopeFactory scopeFactory,
        IPresenceTracker tracker,
        ILogger<PresenceFlushService> logger)
    {
        _scopeFactory = scopeFactory;
        _tracker = tracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        try
        {
            var changed = _tracker.DrainChanged();

            if (changed.Count > 0)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                foreach (var entry in changed)
                {
                    var sessionId = await context.UserSessions
                        .Where(s => s.UserId == entry.UserId)
                        .OrderByDescending(s => s.StartedAt)
                        .Select(s => (Guid?)s.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (sessionId is { } id)
                    {
                        await context.UserSessions
                            .Where(s => s.Id == id && s.LastSeenAt < entry.LastSeenAt)
                            .ExecuteUpdateAsync(
                                u => u.SetProperty(s => s.LastSeenAt, entry.LastSeenAt),
                                cancellationToken);
                    }
                }
            }

            _tracker.Evict(DateTime.UtcNow.AddHours(-1));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Writing last-seen times failed; it will retry.");
        }
    }
}
