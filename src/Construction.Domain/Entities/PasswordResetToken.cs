using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Single-use token for the forgot/reset password flow.
/// Only a SHA-256 hash of the token is stored.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public bool IsValid(DateTime utcNow) => UsedAt is null && ExpiresAt > utcNow;
}
