using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Construction.Infrastructure.Services;

/// <summary>
/// Idempotency keys, kept in the database rather than in memory.
/// </summary>
/// <remarks>
/// <para>
/// In memory would be simpler and wrong twice over: a retry usually arrives at
/// a different instance behind the load balancer, and the case this exists for
/// — the process that took the first request and then died — is exactly the
/// one where an in-process cache has already forgotten.
/// </para>
/// <para>
/// The claim is an insert against a unique index, not a read followed by a
/// write. Two retries that arrive together both find nothing; only the
/// database can decide which of them proceeds, and it does so by refusing the
/// second insert.
/// </para>
/// </remarks>
public class IdempotencyStore : IIdempotencyStore
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTime;

    public IdempotencyStore(ApplicationDbContext context, IDateTimeProvider dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<IdempotencyClaim> ClaimAsync(
        Guid userId,
        string key,
        string endpoint,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        var record = new IdempotencyRecord
        {
            Key = key,
            UserId = userId,
            Endpoint = endpoint,
            RequestHash = requestHash,
            CreatedAt = _dateTime.UtcNow,
        };

        _context.IdempotencyRecords.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            return new IdempotencyClaim(IdempotencyOutcome.Proceed);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // The row is still tracked as Added after a failed save, and the
            // handler's own SaveChanges would try to insert it again — failing
            // the actual request with somebody else's constraint violation.
            _context.Entry(record).State = EntityState.Detached;
        }

        var existing = await _context.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == userId && r.Key == key, cancellationToken);

        if (existing is null)
        {
            // Vanishingly rare: the row was purged between the failed insert
            // and this read. Letting it proceed is the safe end of the trade —
            // a record old enough to be purged is old enough that this is a
            // new request rather than a retry.
            return new IdempotencyClaim(IdempotencyOutcome.Proceed);
        }

        if (existing.RequestHash != requestHash)
        {
            return new IdempotencyClaim(IdempotencyOutcome.Mismatch);
        }

        return existing.CompletedAt is null
            ? new IdempotencyClaim(IdempotencyOutcome.InFlight)
            : new IdempotencyClaim(
                IdempotencyOutcome.Replay,
                existing.StatusCode,
                existing.ResponseBody);
    }

    public async Task CompleteAsync(
        Guid userId,
        string key,
        int statusCode,
        string? responseBody,
        CancellationToken cancellationToken = default)
    {
        // ExecuteUpdate rather than load-and-save: the change tracker at this
        // point is full of whatever the request just wrote, and a stray
        // SaveChanges here would flush it a second time.
        await _context.IdempotencyRecords
            .Where(r => r.UserId == userId && r.Key == key)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.CompletedAt, _dateTime.UtcNow)
                    .SetProperty(r => r.StatusCode, statusCode)
                    .SetProperty(r => r.ResponseBody, responseBody),
                cancellationToken);
    }

    public async Task ReleaseAsync(
        Guid userId,
        string key,
        CancellationToken cancellationToken = default)
    {
        await _context.IdempotencyRecords
            .Where(r => r.UserId == userId && r.Key == key && r.CompletedAt == null)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: "23505" };
}
