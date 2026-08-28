using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One user's first look at one bulletin post — the record an admin reads to
/// see who has (and has not) seen a notice.
/// </summary>
public class BulletinView : BaseEntity
{
    public Guid BulletinPostId { get; set; }

    public BulletinPost BulletinPost { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime ViewedAt { get; set; }
}
