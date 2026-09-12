using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One site's proof-of-work for one ISO calendar week — the signed hours or
/// Aufmaß sheet a foreman collects from the customer and sends to the
/// office, so it lands sorted by site and week instead of in an inbox
/// someone has to organise by hand.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not built on the shared <see cref="Attachment"/> table: every
/// report carries exactly one required file with no category, no expiry and
/// no possibility of more than one, so the fields live directly on this
/// entity instead of threading a sixteenth owner type through
/// <c>Attachment</c>'s shared permission and category rules for a shape that
/// table was not built to express.
/// </para>
/// <para>
/// Billing itself — reading the codes off an Aufmaß sheet, pricing them,
/// raising the invoice — happens in the office's own accounting software.
/// This is the record of what was submitted, not what it was worth.
/// </para>
/// </remarks>
public class WeeklySiteReport : BaseEntity, IAuditable
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid SubmittedByEmployeeId { get; set; }

    public Employee SubmittedByEmployee { get; set; } = null!;

    /// <summary>ISO-8601 week-numbering year — not always the same as the calendar year for late-December/early-January dates.</summary>
    public int IsoYear { get; set; }

    /// <summary>ISO-8601 week number, 1-53.</summary>
    public int IsoWeek { get; set; }

    public WeeklyReportType Type { get; set; }

    /// <summary>
    /// Hours for <see cref="WeeklyReportType.SignedHours"/>. Left null for
    /// Aufmaß and Other — their real breakdown is the itemised codes on the
    /// attached sheet, not one number this platform would have to guess at.
    /// </summary>
    public decimal? Quantity { get; set; }

    public string? Note { get; set; }

    /// <summary>Name as uploaded, shown to people. Never used as a path.</summary>
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    /// <summary>The <see cref="Application.Common.Interfaces.IFileStorage"/> key the proof document's bytes live under.</summary>
    public string StorageKey { get; set; } = null!;

    public WeeklyReportStatus Status { get; set; } = WeeklyReportStatus.Submitted;

    /// <summary>When the office marked this as taken care of (billed, filed — whatever "handled" means to them).</summary>
    public DateTime? ProcessedAt { get; set; }

    public Guid? ProcessedByUserId { get; set; }

    public User? ProcessedByUser { get; set; }
}
