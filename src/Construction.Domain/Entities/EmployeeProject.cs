using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One stretch of time an employee is posted to a project.
/// </summary>
/// <remarks>
/// Was a plain many-to-many with a composite key, which could only answer "is
/// this person on this site" — with no way to say when, and no way to record
/// that they moved to another site on Thursday. A schedule needs both, so the
/// row now carries a date range and its own key.
///
/// Overlapping assignments to <em>different</em> sites are allowed on purpose:
/// a supervisor covering two sites at once is real, and forbidding it would
/// make the system lie about where people are. What is refused is the same
/// person on the same site twice over the same days, which is always a
/// data-entry mistake.
/// </remarks>
public class EmployeeProject : BaseEntity
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    /// <summary>Null while the posting is open-ended.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>When the posting was recorded, as opposed to when it starts.</summary>
    public DateTime AssignedAt { get; set; }

    public Guid? AssignedByUserId { get; set; }

    /// <summary>True when the posting covers the given day.</summary>
    public bool CoversDay(DateOnly day) =>
        StartDate <= day && (EndDate is null || EndDate >= day);
}
