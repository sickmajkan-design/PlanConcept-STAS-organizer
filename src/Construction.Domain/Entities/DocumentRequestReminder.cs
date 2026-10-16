using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Records that a reminder about a request's deadline has been sent, so the daily sweep never repeats it.
/// The row is the claim itself: a unique index on (request, kind, deadline) turns a second sweep into a
/// failed insert rather than a second notice.
/// </summary>
/// <remarks>
/// Scoped to the deadline as it was when claimed, not only to the request: when the office moves the
/// deadline, the new date is worth a fresh reminder, so the claim for the old date must not suppress it.
/// </remarks>
public class DocumentRequestReminder : BaseEntity
{
    public Guid DocumentRequestId { get; set; }

    public DocumentRequest DocumentRequest { get; set; } = null!;

    public DocumentRequestReminderKind Kind { get; set; }

    /// <summary>The <see cref="DocumentRequest.DueDate"/> this claim was made against.</summary>
    public DateOnly DueDate { get; set; }

    public DateTime SentAt { get; set; }
}
