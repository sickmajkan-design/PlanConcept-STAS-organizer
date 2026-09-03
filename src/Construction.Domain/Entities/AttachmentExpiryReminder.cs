using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Records that a specific admin has already been told a specific document
/// is lapsing, so the nightly sweep never repeats itself to them.
/// </summary>
/// <remarks>
/// One row per (document, admin) rather than a single "reminded" flag on
/// <see cref="Attachment"/>, because each admin now sets their own lead time
/// (see <see cref="User.DocumentExpiryReminderDays"/>) — one admin's 45-day
/// warning and another's 10-day warning are two different, independently
/// true events for the same document, and a shared flag could only record
/// one of them.
/// </remarks>
public class AttachmentExpiryReminder : BaseEntity
{
    public Guid AttachmentId { get; set; }

    public Attachment Attachment { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime SentAt { get; set; }
}
