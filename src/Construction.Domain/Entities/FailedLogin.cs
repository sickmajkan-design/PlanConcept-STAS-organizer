using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One refused sign-in: the address that was tried, why it was refused, and
/// from where. <see cref="UserId"/> is empty when no such account exists.
/// </summary>
/// <remarks>
/// Purged after <c>Retention:FailedLoginDays</c>. Visible to SuperAdmin only.
/// </remarks>
public class FailedLogin : BaseEntity
{
    public Guid? UserId { get; set; }

    public string Email { get; set; } = null!;

    /// <summary>UnknownAccount, WrongPassword, LockedOut or Deactivated.</summary>
    public string Reason { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime OccurredAt { get; set; }
}
