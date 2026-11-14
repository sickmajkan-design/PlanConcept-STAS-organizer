using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Writes a heartbeat every minute. When the application starts and the last heartbeat is more than
/// <see cref="OutageAfter"/> old, the gap is recorded as an outage: that is how the Server status page can show a
/// stretch in which the whole server was down, which nothing running on the server could have reported at the time.
/// A normal deploy restart is shorter than the gap and is not counted. One instance is assumed.
/// </summary>
public class UptimeService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    /// <summary>Longer than a deploy restart plus one missed beat; shorter than anything worth knowing about.</summary>
    internal static readonly TimeSpan OutageAfter = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UptimeService> _logger;

    public UptimeService(IServiceScopeFactory scopeFactory, ILogger<UptimeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await BeatAsync(stoppingToken);

            using var timer = new PeriodicTimer(Interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await BeatAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
    }

    private async Task BeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var now = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().UtcNow;

            var beat = await context.ServiceHeartbeats.FirstOrDefaultAsync(h => h.Id == ServiceHeartbeat.SingleId, cancellationToken);

            if (beat is null)
            {
                context.ServiceHeartbeats.Add(new ServiceHeartbeat { Id = ServiceHeartbeat.SingleId, LastBeatAt = now });
            }
            else
            {
                if (now - beat.LastBeatAt > OutageAfter)
                {
                    context.ServiceOutages.Add(new ServiceOutage { StartedAt = beat.LastBeatAt, EndedAt = now });
                }

                beat.LastBeatAt = now;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The database being down is exactly when this must not take the application with it.
            _logger.LogWarning(exception, "Could not write the heartbeat.");
        }
    }
}
