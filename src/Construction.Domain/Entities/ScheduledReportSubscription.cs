using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// "Email me this export every week/month" — a standing order to re-run one
/// of the existing exports on a schedule and mail the result, instead of
/// someone having to remember to click Export.
/// </summary>
public class ScheduledReportSubscription : BaseEntity
{
    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Where the report goes. A free field rather than always the creator's
    /// own address — the person setting this up is often not the accountant
    /// who should receive it.
    /// </summary>
    public string RecipientEmail { get; set; } = null!;

    public ScheduledReportType ReportType { get; set; }

    public ScheduledReportCadence Cadence { get; set; }

    /// <summary>Which weekday, when <see cref="Cadence"/> is <see cref="ScheduledReportCadence.Weekly"/>.</summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>
    /// Which day of the month (1-28), when <see cref="Cadence"/> is
    /// <see cref="ScheduledReportCadence.Monthly"/>. Capped at 28 so every
    /// month actually has the day — no "the 31st" that skips half the year.
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>`sr` or `en` — the language the spreadsheet headings render in.</summary>
    public string Language { get; set; } = "sr";

    /// <summary>
    /// When this becomes due. Advanced by the sweep after every attempt,
    /// success or failure, so a broken subscription is retried on its next
    /// occurrence rather than every sweep forever.
    /// </summary>
    public DateTime NextRunAtUtc { get; set; }
}
