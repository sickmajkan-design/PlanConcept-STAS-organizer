using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One sending of an announcement: what was said, to whom it was addressed and by whom. The inbox rows
/// (<see cref="Notification"/>) of the people it reached point back at it, which is how the sender sees who has read it.
/// </summary>
public class NotificationBroadcast : BaseEntity
{
    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    /// <summary>Whether each recipient had to confirm they had read it.</summary>
    public bool RequiresAcknowledgment { get; set; }

    public Guid? SentByUserId { get; set; }

    public User? SentByUser { get; set; }

    /// <summary>What was chosen as the audience (the chips): kind, the thing chosen and how many people it stood for.</summary>
    public string AudienceJson { get; set; } = "[]";

    /// <summary>How many people it reached.</summary>
    public int RecipientCount { get; set; }

    /// <summary>People the choice covered who have no account, so nothing could be sent to them.</summary>
    public int WithoutAccountCount { get; set; }

    public DateTime? LastReminderAt { get; set; }
}
