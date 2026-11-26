using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Planning;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Puts the equipment changes a planner decided on for a later day into effect when the day comes. Idempotent: an applied
/// change is gone, so two instances running it together do no harm.
/// </summary>
public class PlannedEquipmentService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlannedEquipmentService> _logger;

    public PlannedEquipmentService(IServiceScopeFactory scopeFactory, ILogger<PlannedEquipmentService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

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
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var applied = await PlannedEquipmentApplier.ApplyDueAsync(context, CompanyTime.LocalDate(DateTime.UtcNow), cancellationToken);

            if (applied > 0)
            {
                _logger.LogInformation("Applied {Count} planned equipment change(s).", applied);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Applying planned equipment changes failed; it will be retried.");
        }
    }
}
