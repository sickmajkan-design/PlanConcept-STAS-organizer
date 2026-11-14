using Construction.Application.Features.SystemStatus;
using MediatR;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Every few minutes, looks at what is wrong with the server and tells the Super Admins by the channels they chose
/// (see <see cref="SendServerAlertsCommand"/>). The same problem is repeated at most every six hours.
/// </summary>
public class ServerAlertService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    /// <summary>Gives the application time to finish starting, and the uptime service to record a gap, before the first look.</summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServerAlertService> _logger;

    public ServerAlertService(IServiceScopeFactory scopeFactory, ILogger<ServerAlertService> logger)
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
            // Shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sent = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new SendServerAlertsCommand(), cancellationToken);

            if (sent > 0)
            {
                _logger.LogWarning("Told {Count} Super Admin(s) about trouble on the server.", sent);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "The server alert check failed; the next one will retry.");
        }
    }
}
