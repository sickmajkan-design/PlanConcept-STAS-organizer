namespace Construction.Application.Common.Interfaces;

/// <summary>What claiming an idempotency key produced.</summary>
public enum IdempotencyOutcome
{
    /// <summary>The key is new. Run the request.</summary>
    Proceed,

    /// <summary>The key has been used and finished. Replay the stored answer.</summary>
    Replay,

    /// <summary>
    /// The key was claimed and has not finished. The first attempt is still
    /// running, or the process handling it died.
    /// </summary>
    InFlight,

    /// <summary>
    /// The key has been used for a *different* request. A client bug, and the
    /// one case where doing nothing would silently drop somebody's work.
    /// </summary>
    Mismatch,
}

/// <summary>
/// The outcome, plus the stored response when there is one to replay.
/// </summary>
public record IdempotencyClaim(
    IdempotencyOutcome Outcome,
    int? StatusCode = null,
    string? ResponseBody = null);

/// <summary>
/// Remembers which requests have already been carried out, so a retry cannot
/// carry them out again.
/// </summary>
/// <remarks>
/// An interface in the Application layer, because the mechanism is a database
/// unique constraint and only Infrastructure knows about those — and because a
/// test wants to be able to watch two callers race.
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    /// Claims <paramref name="key"/> for this caller, or reports what has
    /// already happened under it.
    /// </summary>
    Task<IdempotencyClaim> ClaimAsync(
        Guid userId,
        string key,
        string endpoint,
        string requestHash,
        CancellationToken cancellationToken = default);

    /// <summary>Records what the first attempt returned, so a retry can be answered.</summary>
    Task CompleteAsync(
        Guid userId,
        string key,
        int statusCode,
        string? responseBody,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives the key back, because the attempt failed and a retry should be
    /// allowed to try again rather than be handed the failure for ever.
    /// </summary>
    Task ReleaseAsync(Guid userId, string key, CancellationToken cancellationToken = default);
}
