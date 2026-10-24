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

    /// <summary>
    /// Set by the sender when the recipient must explicitly confirm they saw
    /// this before doing anything else — a hazard notice, a safety recall.
    /// Distinct from <see cref="IsRead"/>, which just means it was opened.
    /// </summary>
    public bool RequiresAcknowledgment { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>The announcement this inbox row belongs to, when it was sent as one; null for notices the system raised.</summary>
    public Guid? BroadcastId { get; set; }

    public NotificationBroadcast? Broadcast { get; set; }

    /// <summary>For an announcement: through which choice of the audience this person was reached ("Projekat: Hala B").</summary>
    public string? Via { get; set; }

    /// <summary>A repeat of the announcement sent to those who had not read it; not counted among the first deliveries.</summary>
    public bool IsReminder { get; set; }
}
