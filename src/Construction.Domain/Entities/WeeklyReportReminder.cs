using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Records that a specific employee has already been reminded about a
/// specific (site, ISO week) with no report yet, so the sweep never repeats
/// itself even if it runs more than once before the week is over.
/// </summary>
public class WeeklyReportReminder : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int IsoYear { get; set; }

    public int IsoWeek { get; set; }

    public DateTime SentAt { get; set; }
}
