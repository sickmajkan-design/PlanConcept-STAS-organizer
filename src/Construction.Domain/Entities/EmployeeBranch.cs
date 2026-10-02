using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One stretch of time an employee is employed in a business unit (poslovna jedinica) — the unit
/// that is their employer, whichever project they are posted to.
/// </summary>
/// <remarks>
/// Dated like a project posting, because a person moves between units and what was worked before
/// the move belongs to the unit they were then in: hours and pay are attributed by the unit that
/// covers the day, never by where the employee is today. Periods of one employee never overlap;
/// the open-ended one (no <see cref="EndDate"/>) is where they are now. An employee with no period
/// covering a day belonged to no unit that day.
///
/// This is separate from <see cref="Project.BranchId"/> on purpose: the unit that employs someone
/// and the unit whose site they work on are two different facts, and both matter.
/// </remarks>
public class EmployeeBranch : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Guid BranchId { get; set; }

    public Branch Branch { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    /// <summary>Null while the employment in the unit is ongoing.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>True when the employment covers the given day.</summary>
    public bool CoversDay(DateOnly day) =>
        StartDate <= day && (EndDate is null || EndDate >= day);
}
