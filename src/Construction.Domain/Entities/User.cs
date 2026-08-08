using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Application account used for authentication and authorization.
/// A user may optionally be linked to an <see cref="Employee"/> record.
/// </summary>
public class User : BaseEntity, IAuditable
{
    public string Email { get; set; } = null!;

    [NotAudited]
    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Consecutive failed sign-ins since the last success. Reset on any
    /// successful sign-in or password change.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// When set and in the future, sign-in is refused regardless of the
    /// password. Tracked per account rather than per address because an
    /// attacker chooses their address but not the account they are attacking.
    /// </summary>
    public DateTime? LockoutEndsAt { get; set; }

    /// <summary>Whether sign-in is currently barred by lockout.</summary>
    public bool IsLockedOut(DateTime utcNow) => LockoutEndsAt is { } until && until > utcNow;

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
