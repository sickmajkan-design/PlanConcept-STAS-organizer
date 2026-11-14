using Construction.Application.Features.SystemStatus;
using MediatR;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Writes the errors collected in memory (<see cref="ServerErrorBuffer"/>) into the database once a minute, and about
/// once an hour deletes the ones older than 30 days. Tries once more when the application stops, so a restart does not
/// lose the last minute.
/// </summary>
public class ServerErrorService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private const int PruneEvery = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServerErrorService> _logger;
    private int _runs;

    public ServerErrorService(IServiceScopeFactory scopeFactory, ILogger<ServerErrorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(Interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(prune: ++_runs % PruneEvery == 0, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(prune: false, CancellationToken.None);
    }

    private async Task FlushAsync(bool prune, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new FlushServerErrorsCommand(prune), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The entries went back into the buffer; the next minute tries again.
            _logger.LogWarning(exception, "Could not save the server error counts.");
        }
    }
}
