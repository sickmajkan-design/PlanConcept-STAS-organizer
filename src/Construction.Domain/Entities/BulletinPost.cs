using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A notice pinned to the company bulletin board — visible to everyone,
/// including people hired after it was posted, until an admin removes it.
/// </summary>
/// <remarks>
/// Deliberately not modeled on <see cref="Notification"/>: a notification is
/// a copy handed to each recipient at send time, so someone who did not exist
/// yet never gets one. A bulletin is the opposite — one row everybody reads
/// live off of, for as long as it stands.
/// </remarks>
public class BulletinPost : BaseEntity, IAuditable
{
    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public ICollection<BulletinView> Views { get; set; } = new List<BulletinView>();
}
