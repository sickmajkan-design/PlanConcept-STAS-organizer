using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Firebase Cloud Messaging registration token for one of a user's devices.
/// </summary>
public class DeviceToken : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DevicePlatform Platform { get; set; }

    public DateTime? LastUsedAt { get; set; }
}
