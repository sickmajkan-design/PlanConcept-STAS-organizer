using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// The half-finished sign-in of an account that has a second factor: the
/// password was right, the code has not been given yet. Short-lived,
/// single-use, and limited in attempts so a six-digit code cannot be guessed.
/// Only a SHA-256 hash of the token is stored.
/// </summary>
public class MfaChallenge : BaseEntity
{
    /// <summary>Wrong codes allowed before the challenge is spent.</summary>
    public const int MaxAttempts = 5;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public int Attempts { get; set; }

    public DateTime? UsedAt { get; set; }

    public bool IsOpen(DateTime utcNow) =>
        UsedAt is null && ExpiresAt > utcNow && Attempts < MaxAttempts;
}
