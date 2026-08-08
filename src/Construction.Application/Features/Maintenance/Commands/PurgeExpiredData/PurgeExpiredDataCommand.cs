using Construction.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Maintenance.Commands.PurgeExpiredData;

/// <summary>What one sweep removed.</summary>
public record PurgeResult(
    int RefreshTokens,
    int PasswordResetTokens,
    int LocationRecords,
    int OutboxMessages,
    int AuditEntries,
    int TimeEntryCoordinates)
{
    public int Total =>
        RefreshTokens + PasswordResetTokens + LocationRecords + OutboxMessages
        + AuditEntries + TimeEntryCoordinates;
}

/// <summary>
/// Deletes the rows the system has finished with: spent tokens, and GPS pings
/// older than the retention window.
/// </summary>
/// <remarks>
/// <para>
/// Nothing else ever removes these. Tokens are small but unbounded;
/// <c>location_records</c> is neither — one ping a minute per person is around
/// a million rows a month for a hundred workers. Reads stay fast because the
/// index carries them, so the table does not announce itself by getting slow.
/// It announces itself in the backup that takes an hour, the restore nobody
/// has rehearsed, and the disk that fills on a Saturday.
/// </para>
/// <para>
/// There is a second reason, and it is the one that matters legally: a record
/// of where an employee was, minute by minute, is personal data. Keeping it
/// forever is a decision, and it should be a deliberate one with a number
/// attached rather than the default that falls out of never having written
/// the delete.
/// </para>
/// </remarks>
public record PurgeExpiredDataCommand : IRequest<PurgeResult>
{
    /// <summary>
    /// How long a spent refresh token is kept after it expires.
    /// </summary>
    /// <remarks>
    /// Not zero, and not "delete revoked rows". Rotation leaves the old row
    /// behind on purpose: presenting it again is how the API detects a stolen
    /// token and revokes every session for that account. Delete the row and a
    /// replay becomes an ordinary unknown token — refused, but silently, with
    /// the theft signal gone. Rows past their own expiry can no longer
    /// authenticate anything, so those are safe to drop; the grace period is
    /// there so the audit trail outlives the incident that would send somebody
    /// looking for it.
    /// </remarks>
    public TimeSpan RefreshTokenGrace { get; init; } = TimeSpan.FromDays(30);

    /// <summary>How long a used or expired reset token is kept.</summary>
    public TimeSpan PasswordResetTokenGrace { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// How long a GPS ping is kept. Null keeps them forever.
    /// </summary>
    /// <remarks>
    /// Null is offered because a deployment may be under an obligation to
    /// retain, not because it is a sensible default. It is not the default.
    /// </remarks>
    public TimeSpan? LocationRecordRetention { get; init; } = TimeSpan.FromDays(180);

    /// <summary>
    /// How long a delivered outbox message is kept.
    /// </summary>
    /// <remarks>
    /// Long enough to answer "was that email actually sent, and when?" for a
    /// week or two after somebody asks. Abandoned messages are never purged
    /// here: there are few of them and each one is a delivery that failed for
    /// good, which is exactly the thing somebody will eventually need to look
    /// at.
    /// </remarks>
    public TimeSpan SentOutboxMessageRetention { get; init; } = TimeSpan.FromDays(14);

    /// <summary>
    /// How long an audit entry is kept. Null — the default — keeps them
    /// forever.
    /// </summary>
    /// <remarks>
    /// The only retention here that defaults to keeping, because the trail
    /// exists for the dispute that surfaces years later. Setting a number is
    /// a decision to delete evidence on a schedule, which some deployments
    /// are obliged to make and none should make by accident.
    /// </remarks>
    public TimeSpan? AuditEntryRetention { get; init; }

    /// <summary>
    /// How long a shift's clock-in and clock-out coordinates are kept. Null —
    /// the default — keeps them for as long as the shift itself.
    /// </summary>
    /// <remarks>
    /// The coordinates deliberately outlive the GPS sweep, because an approved
    /// timesheet is payroll evidence and where the shift started is part of it.
    /// This is the way to bound that for a deployment that needs to: the
    /// coordinates are cleared and the shift is left alone, so the hours still
    /// add up.
    /// </remarks>
    public TimeSpan? TimeEntryCoordinateRetention { get; init; }

    /// <summary>
    /// Rows deleted per statement.
    /// </summary>
    /// <remarks>
    /// A single unbounded <c>DELETE</c> over a year of pings would hold locks
    /// and a transaction open for minutes, block autovacuum on the table it is
    /// bloating, and roll the whole thing back if it were interrupted.
    /// Batching means an interrupted sweep has still made progress and the
    /// next one carries on.
    /// </remarks>
    public int BatchSize { get; init; } = 5_000;

    /// <summary>
    /// Statements per table per sweep, so one run cannot spin forever.
    /// </summary>
    /// <remarks>
    /// The first sweep against a table that has never been purged has a
    /// backlog to work through. It works through it over several runs rather
    /// than in one that competes with live traffic for an unbounded stretch.
    /// </remarks>
    public int MaxBatchesPerTable { get; init; } = 20;
}

public class PurgeExpiredDataCommandValidator : AbstractValidator<PurgeExpiredDataCommand>
{
    public PurgeExpiredDataCommandValidator()
    {
        RuleFor(x => x.RefreshTokenGrace)
            .Must(grace => grace >= TimeSpan.Zero)
            .WithMessage("The refresh-token grace period cannot be negative.");

        RuleFor(x => x.PasswordResetTokenGrace)
            .Must(grace => grace >= TimeSpan.Zero)
            .WithMessage("The reset-token grace period cannot be negative.");

        // A retention of zero would delete a ping the moment it arrived, which
        // is indistinguishable from tracking being broken.
        RuleFor(x => x.LocationRecordRetention)
            .Must(retention => retention is null || retention > TimeSpan.Zero)
            .WithMessage("Location retention must be a positive period, or unset to keep everything.");

        RuleFor(x => x.TimeEntryCoordinateRetention)
            .Must(retention => retention is null || retention > TimeSpan.Zero)
            .WithMessage("Time-entry coordinate retention must be a positive period, or unset to keep them.");

        RuleFor(x => x.AuditEntryRetention)
            .Must(retention => retention is null || retention > TimeSpan.Zero)
            .WithMessage("Audit retention must be a positive period, or unset to keep everything.");

        RuleFor(x => x.SentOutboxMessageRetention)
            .Must(retention => retention >= TimeSpan.Zero)
            .WithMessage("The outbox retention period cannot be negative.");

        RuleFor(x => x.BatchSize).InclusiveBetween(1, 100_000);

        RuleFor(x => x.MaxBatchesPerTable).InclusiveBetween(1, 1_000);
    }
}

public class PurgeExpiredDataCommandHandler
    : IRequestHandler<PurgeExpiredDataCommand, PurgeResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PurgeExpiredDataCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PurgeResult> Handle(
        PurgeExpiredDataCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;

        var refreshTokens = await DeleteInBatchesAsync(
            _context.RefreshTokens
                .Where(t => t.ExpiresAt < utcNow - request.RefreshTokenGrace),
            request,
            cancellationToken);

        var resetTokens = await DeleteInBatchesAsync(
            _context.PasswordResetTokens
                .Where(t => t.ExpiresAt < utcNow - request.PasswordResetTokenGrace),
            request,
            cancellationToken);

        var locations = 0;

        if (request.LocationRecordRetention is { } retention)
        {
            // On Timestamp, the moment the device says it was there, rather
            // than ReceivedAt: a phone that was offline for a week uploads a
            // week-old ping, and the age that matters is the age of the fact.
            locations = await DeleteInBatchesAsync(
                _context.LocationRecords.Where(r => r.Timestamp < utcNow - retention),
                request,
                cancellationToken);
        }

        // Sent only. An abandoned message is the record of a delivery that
        // failed for good, and deleting those would leave the question
        // "why did they never get the email?" with nothing to answer it.
        var outbox = await DeleteInBatchesAsync(
            _context.OutboxMessages
                .Where(m => m.SentAt != null
                    && m.SentAt < utcNow - request.SentOutboxMessageRetention),
            request,
            cancellationToken);

        var audit = 0;

        if (request.AuditEntryRetention is { } auditRetention)
        {
            audit = await DeleteInBatchesAsync(
                _context.AuditEntries.Where(a => a.OccurredAt < utcNow - auditRetention),
                request,
                cancellationToken);
        }

        var coordinates = 0;

        if (request.TimeEntryCoordinateRetention is { } coordinateRetention)
        {
            // Cleared, not deleted: the shift is payroll and stays. Only the
            // two coordinates hanging off it go.
            coordinates = await _context.TimeEntries
                .IgnoreQueryFilters()
                .Where(t => t.StartedAt < utcNow - coordinateRetention
                    && (t.StartLatitude != null || t.EndLatitude != null))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.StartLatitude, (double?)null)
                    .SetProperty(t => t.StartLongitude, (double?)null)
                    .SetProperty(t => t.EndLatitude, (double?)null)
                    .SetProperty(t => t.EndLongitude, (double?)null), cancellationToken);
        }

        return new PurgeResult(
            refreshTokens, resetTokens, locations, outbox, audit, coordinates);
    }

    /// <summary>
    /// Deletes matching rows a batch at a time, stopping when a batch comes
    /// back short — which means there was nothing left to match.
    /// </summary>
    private static async Task<int> DeleteInBatchesAsync<T>(
        IQueryable<T> query,
        PurgeExpiredDataCommand request,
        CancellationToken cancellationToken)
        where T : class
    {
        var deleted = 0;

        for (var batch = 0; batch < request.MaxBatchesPerTable; batch++)
        {
            var removed = await query
                .Take(request.BatchSize)
                .ExecuteDeleteAsync(cancellationToken);

            deleted += removed;

            if (removed < request.BatchSize)
            {
                break;
            }
        }

        return deleted;
    }
}
