using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Application account used for authentication and authorization.
/// A user may optionally be linked to an <see cref="Employee"/> record.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
