using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Records that a specific admin has already been told a specific document's
/// retention period has ended, so the nightly sweep never repeats itself to
/// them.
/// </summary>
/// <remarks>
/// Mirrors <see cref="AttachmentExpiryReminder"/> exactly, and is kept as its
/// own table rather than folded into it for the same reason that one exists
/// on its own: "this document is about to lapse" and "this document may now
/// be deleted" are unrelated events that happen to share a document, and a
/// document can have both, one, or neither pending at once.
/// </remarks>
public class AttachmentRetentionReminder : BaseEntity
{
    public Guid AttachmentId { get; set; }

    public Attachment Attachment { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime SentAt { get; set; }
}
