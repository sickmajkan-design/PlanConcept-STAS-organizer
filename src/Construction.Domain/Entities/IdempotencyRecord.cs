namespace Construction.Domain.Entities;

/// <summary>
/// One request that must not happen twice, and what happened the first time.
/// </summary>
/// <remarks>
/// <para>
/// The problem this exists for: a foreman on a site taps "consume 40 bags",
/// the response is lost on the way back, and the app — or the foreman —
/// retries. The second request is indistinguishable from a genuine second
/// consumption, so the stock drops by eighty. Nothing in HTTP prevents this;
/// the client has to name the attempt, and the server has to remember the
/// name.
/// </para>
/// <para>
/// Deliberately not a <c>BaseEntity</c>. A record is written once, completed
/// once, and then only read or purged — soft delete and <c>UpdatedAt</c> would
/// be meaningless, and a soft-deleted record would still have to be found by
/// the unique index to do its job, which is exactly what the global query
/// filter would stop.
/// </para>
/// </remarks>
public class IdempotencyRecord
{
    public Guid Id { get; set; }

    /// <summary>The client's name for this attempt, from <c>Idempotency-Key</c>.</summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Who sent it. Part of the unique index, so one account's key can never
    /// return another account's stored response — the reply may contain data
    /// the second caller is not allowed to see.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>Method and path, e.g. <c>POST /api/v1/materials/{id}/adjust</c>.</summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>
    /// SHA-256 of the route and body the key was first used with.
    /// </summary>
    /// <remarks>
    /// A key reused for a different request is a client bug — a key generated
    /// once and pinned to a screen rather than to an action — and returning
    /// the first response to it would silently drop the second. Refused
    /// instead. The hash rather than the body because the body may contain
    /// personal data and this table is not the place to keep a second copy of
    /// it.
    /// </remarks>
    public string RequestHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the first attempt finished. Null means it is still running — or
    /// that the process handling it died.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>The status the first attempt returned, once it has one.</summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// The body the first attempt returned, replayed verbatim to a retry.
    /// </summary>
    /// <remarks>
    /// Stored rather than recomputed. The point of a replay is that the second
    /// caller sees exactly what the first would have seen; re-running the query
    /// would return the state as it is now, which may have moved on — and a
    /// retry that reports a different quantity than the one it caused is worse
    /// than no idempotency at all.
    /// </remarks>
    public string? ResponseBody { get; set; }
}
