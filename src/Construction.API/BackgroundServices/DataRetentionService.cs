using System.Diagnostics;
using Construction.API.Observability;
using Construction.Application.Features.Maintenance.Commands.PurgeExpiredData;
using MediatR;
using Microsoft.Extensions.Options;

namespace Construction.API.BackgroundServices;

/// <summary>
/// Sweeps away spent tokens and GPS pings past their retention window.
/// </summary>
/// <remarks>
/// <para>
/// Every four to six hours rather than daily, because the point is to keep the
/// backlog small: a sweep that runs often finds a few thousand rows and
/// finishes in milliseconds, while one that runs once a day against a busy
/// deployment finds a hundred thousand and has to work through them in batches
/// over several runs anyway.
/// </para>
/// <para>
/// Two API instances both running this is harmless. Each batch is a single
/// <c>DELETE</c> bounded by <c>LIMIT</c>; if both run at once they delete
/// disjoint sets or one finds the rows already gone, and the counts they log
/// are the counts they actually removed. There is nothing to claim first,
/// because a deleted row cannot be deleted twice.
/// </para>
/// </remarks>
public class DataRetentionService : BackgroundService
{
    /// <summary>
    /// Delay before the first sweep, so startup is not competing with
    /// migrations and the first requests for a connection.
    /// </summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetentionSettings _settings;
    private readonly JobMetrics _metrics;
    private readonly ILogger<DataRetentionService> _logger;

    public DataRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<RetentionSettings> settings,
        JobMetrics metrics,
        ILogger<DataRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_settings.LocationRecordDays <= 0)
        {
            // Said once, at startup, rather than on every sweep. Somebody
            // reading the logs to work out why the table keeps growing should
            // find the answer without having to guess that a setting exists.
            _logger.LogWarning(
                "Location retention is disabled ({Setting}:{Key} is {Value}), so GPS pings "
                + "will be kept indefinitely. This is personal data; set a retention period "
                + "unless there is an obligation to keep it.",
                RetentionSettings.SectionName,
                nameof(RetentionSettings.LocationRecordDays),
                _settings.LocationRecordDays);
        }

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using var timer = new PeriodicTimer(
                TimeSpan.FromHours(Math.Max(1, _settings.IntervalHours)));

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

            var result = await mediator.Send(
                new PurgeExpiredDataCommand
                {
                    RefreshTokenGrace = TimeSpan.FromDays(_settings.RefreshTokenGraceDays),
                    PasswordResetTokenGrace =
                        TimeSpan.FromDays(_settings.PasswordResetTokenGraceDays),
                    LocationRecordRetention = _settings.LocationRetention,
                    AuditEntryRetention = _settings.AuditRetention,
                    SentOutboxMessageRetention =
                        TimeSpan.FromDays(_settings.SentOutboxMessageDays),
                    BatchSize = _settings.BatchSize,
                    MaxBatchesPerTable = _settings.MaxBatchesPerTable,
                },
                cancellationToken);

            _metrics.Purged("refresh_tokens", result.RefreshTokens);
            _metrics.Purged("password_reset_tokens", result.PasswordResetTokens);
            _metrics.Purged("location_records", result.LocationRecords);
            _metrics.Purged("outbox_messages", result.OutboxMessages);
            _metrics.Purged("audit_entries", result.AuditEntries);

            // Only when it did something. A line every six hours saying
            // "removed nothing" is noise that trains people to skip the ones
            // that matter.
            if (result.Total > 0)
            {
                _logger.LogInformation(
                    "Retention sweep removed {RefreshTokens} refresh token(s), "
                    + "{ResetTokens} reset token(s), {Locations} location record(s), "
                    + "{OutboxMessages} delivered message(s) and {AuditEntries} audit entry(s).",
                    result.RefreshTokens,
                    result.PasswordResetTokens,
                    result.LocationRecords,
                    result.OutboxMessages,
                    result.AuditEntries);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A failed sweep must not take the host down. Nothing was half
            // done — each batch is its own statement — so the next run simply
            // finds the same rows still waiting.
            _metrics.JobFailed("retention");

            _logger.LogError(
                exception, "The retention sweep failed; it will run again.");
        }
        finally
        {
            _metrics.JobFinished("retention", Stopwatch.GetElapsedTime(started));
        }
    }
}
