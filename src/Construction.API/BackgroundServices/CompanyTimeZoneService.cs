using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Keeps the platform's calendar in the zone the organisation chose.
/// </summary>
/// <remarks>
/// The zone is a setting of the organisation, kept in its company settings. It is read when the application starts and
/// again every minute, so a change made on another instance reaches this one without a restart; on the instance where
/// it was made, saving applies it at once. An organisation that has not chosen one keeps the platform's default.
/// </remarks>
public class CompanyTimeZoneService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompanyTimeZoneService> _logger;

    public CompanyTimeZoneService(IServiceScopeFactory scopeFactory, ILogger<CompanyTimeZoneService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RefreshAsync(stoppingToken);

            using var timer = new PeriodicTimer(Interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var chosen = await context.CompanySettings
                .AsNoTracking()
                .Select(s => s.TimeZoneId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(chosen) && !string.Equals(chosen, CompanyTime.ZoneId, StringComparison.Ordinal))
            {
                CompanyTime.Configure(chosen, out var problem);

                if (problem is not null)
                {
                    _logger.LogWarning("{Problem}", problem);
                }
                else
                {
                    _logger.LogInformation("The organisation's time zone is {Zone}.", CompanyTime.ZoneId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The database may not be up yet at start-up; the next tick tries again, and the default stands meanwhile.
            _logger.LogWarning(ex, "Could not read the organisation's time zone.");
        }
    }
}
