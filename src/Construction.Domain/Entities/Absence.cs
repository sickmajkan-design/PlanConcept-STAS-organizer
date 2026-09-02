using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A stretch of days an employee is not at work.
/// </summary>
/// <remarks>
/// The other half of a schedule. Knowing who is posted to a site is only
/// useful alongside knowing who is actually going to turn up: a board that
/// shows a person on site during their annual leave is worse than no board,
/// because somebody plans around it.
///
/// Only an <see cref="AbsenceStatus.Approved"/> absence makes someone
/// unavailable. A request that has not been answered is a question, not a
/// fact, and the schedule must not act on it.
/// </remarks>
public class Absence : BaseEntity, ISoftDeletable, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public AbsenceType Type { get; set; } = AbsenceType.AnnualLeave;

    public AbsenceStatus Status { get; set; } = AbsenceStatus.Requested;

    public DateOnly StartDate { get; set; }

    /// <summary>Inclusive: a single day off has the same start and end.</summary>
    public DateOnly EndDate { get; set; }

    public string? Reason { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public User? RequestedByUser { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Why it was refused. Cleared when it is granted.</summary>
    public string? ReviewNote { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// A change to an already-approved absence's dates/reason, waiting on the
    /// other side to confirm it — see <c>ProposeAbsenceEditCommand</c> and
    /// <c>ConfirmAbsenceEditCommand</c>. Null fields mean no edit is pending;
    /// nothing here touches <see cref="StartDate"/>/<see cref="EndDate"/>
    /// until it is confirmed, so the schedule and leave balance never see a
    /// change nobody has agreed to yet.
    /// </summary>
    public DateOnly? ProposedStartDate { get; set; }

    public DateOnly? ProposedEndDate { get; set; }

    public string? ProposedReason { get; set; }

    public Guid? ProposedByUserId { get; set; }

    public User? ProposedByUser { get; set; }

    /// <summary>
    /// True when the employee themselves proposed the change (so it is
    /// waiting on management to confirm); false when management proposed it
    /// (so it is waiting on the employee).
    /// </summary>
    public bool ProposedByEmployee { get; set; }

    public DateTime? ProposedAt { get; set; }

    public bool HasPendingEdit => ProposedStartDate is not null;

    /// <summary>
    /// Whole days off, both ends included.
    /// </summary>
    public int DayCount => EndDate.DayNumber - StartDate.DayNumber + 1;

    /// <summary>True when this absence keeps the employee away on the given day.</summary>
    public bool CoversDay(DateOnly day) =>
        Status == AbsenceStatus.Approved && StartDate <= day && EndDate >= day;
}
