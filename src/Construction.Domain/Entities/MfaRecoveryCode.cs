using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One single-use code that lets somebody who has lost their authenticator
/// finish a sign-in. Only a SHA-256 hash is stored, like a reset token.
/// </summary>
public class MfaRecoveryCode : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string CodeHash { get; set; } = null!;

    public DateTime? UsedAt { get; set; }
}
