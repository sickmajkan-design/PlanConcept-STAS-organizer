using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Ends a subcontractor's engagement when its end date has passed: the person becomes Terminated and
/// their sign-in is switched off. History (hours, costs, assignments) stays untouched.
/// </summary>
/// <remarks>
/// Idempotent, so two instances running it at once is harmless. Extending the engagement later is done
/// by hand (new end date, status back to Active, account re-activated) on purpose: bringing a
/// sign-in back is a decision, not a side effect of editing a date.
/// </remarks>
public class ContractorExpiryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContractorExpiryService> _logger;

    public ContractorExpiryService(IServiceScopeFactory scopeFactory, ILogger<ContractorExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

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
            var today = CompanyTime.LocalDate(DateTime.UtcNow);

            var expired = context.Employees.Where(e =>
                e.Type == EmployeeType.Subcontractor
                && e.EngagementEndDate != null
                && e.EngagementEndDate < today
                && e.Status != EmployeeStatus.Terminated);

            var accounts = await context.Users
                .Where(u => u.IsActive && u.EmployeeId != null && expired.Any(e => e.Id == u.EmployeeId))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false), cancellationToken);

            var people = await expired
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, EmployeeStatus.Terminated), cancellationToken);

            if (people > 0)
            {
                _logger.LogInformation(
                    "Ended {People} subcontractor engagement(s) and switched off {Accounts} sign-in(s).", people, accounts);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ending expired subcontractor engagements failed; it will be retried.");
        }
    }
}
