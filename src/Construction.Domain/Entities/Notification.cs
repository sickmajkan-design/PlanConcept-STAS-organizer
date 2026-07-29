using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Persisted copy of a push notification, so users can review
/// notifications in-app even if the FCM delivery was missed.
/// </summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    /// <summary>Optional JSON payload with deep-link data (entity ids etc.).</summary>
    public string? DataJson { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }
}
