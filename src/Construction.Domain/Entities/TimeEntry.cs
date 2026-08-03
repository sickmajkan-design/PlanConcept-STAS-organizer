using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One stretch of work by one employee: when it started, when it ended, and
/// whether anyone has signed it off.
/// </summary>
/// <remarks>
/// A running shift is a row with no <see cref="EndedAt"/> rather than a
/// separate "current shift" table, so clocking out is an update instead of a
/// move between tables — and a shift can never exist in both places at once.
/// The database enforces at most one such row per employee.
/// </remarks>
public class TimeEntry : BaseEntity, ISoftDeletable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Nullable: yard work, workshop time and travel are real hours that
    /// belong to no site.
    /// </summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public DateTime StartedAt { get; set; }

    /// <summary>Null while the shift is still running.</summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>Unpaid break, subtracted from the elapsed time.</summary>
    public int BreakMinutes { get; set; }

    public WorkType WorkType { get; set; } = WorkType.Regular;

    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.InProgress;

    public string? Note { get; set; }

    /// <summary>
    /// Where the worker was when they clocked in and out, when the device had
    /// a fix. Recorded on the entry itself rather than looked up from
    /// <see cref="LocationRecord"/> later, because location history is pruned
    /// on a retention schedule while approved hours are kept for payroll.
    /// </summary>
    public double? StartLatitude { get; set; }

    public double? StartLongitude { get; set; }

    public double? EndLatitude { get; set; }

    public double? EndLongitude { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Why the entry was sent back. Cleared when it is approved.</summary>
    public string? ReviewNote { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Paid minutes, or null while the shift is still running.
    /// Never negative: validation rejects a break longer than the shift.
    /// </summary>
    public int? WorkedMinutes => EndedAt is null
        ? null
        : (int)(EndedAt.Value - StartedAt).TotalMinutes - BreakMinutes;

    /// <summary>An approved entry is payroll evidence and no longer editable.</summary>
    public bool IsLocked => Status == TimeEntryStatus.Approved;
}
