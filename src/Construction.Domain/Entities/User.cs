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

    /// <summary>
    /// How many days before a document lapses this admin wants to hear about
    /// it. Only meaningful for <see cref="UserRole.SuperAdmin"/>/
    /// <see cref="UserRole.Admin"/> — the only roles the sweep ever notifies.
    /// Null means the system default (see
    /// <c>SendExpiryRemindersCommand.DefaultReminderDays</c>).
    /// </summary>
    public int? DocumentExpiryReminderDays { get; set; }

    /// <summary>
    /// Whether this account may see a customer's tax ID, registration number
    /// and VAT number. False by default — a SuperAdmin always sees these
    /// regardless of this flag; anyone else only once one grants it here.
    /// </summary>
    public bool CanViewCustomerTaxDetails { get; set; }

    /// <summary>
    /// ISO 639-1 code ("sr", "en") for the language a push notification's
    /// text should be rendered in. Null means unset — the same "assume
    /// Serbian" default the mobile app itself falls back to for a device
    /// whose own locale it does not ship (see mobile's <c>resolveLocale</c>),
    /// since the people using this are on sites in the region.
    /// </summary>
    public string? PreferredLanguage { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
