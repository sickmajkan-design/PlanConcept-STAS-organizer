using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// What an hour of this employee's time costs, over a stretch of time.
/// </summary>
/// <remarks>
/// A rate is dated rather than a column on <see cref="Employee"/> because a
/// cost report is about the past. Everyone got a raise in June; the March
/// report must still say what March cost. A single current-rate column would
/// silently rewrite every report that was ever run whenever anyone's pay
/// changed — and the number would keep looking plausible, which is what makes
/// it dangerous.
///
/// It is the cost to the company per hour, not the wage: the useful number
/// for pricing a job is what the hour costs once contributions are included.
/// What goes into it is the office's decision; the model only stores it.
/// </remarks>
public class EmployeeRate : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>Cost per hour, in the system's single currency.</summary>
    public decimal HourlyRate { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null means it is the rate in force, with no end set.</summary>
    public DateOnly? EndDate { get; set; }

    public string? Note { get; set; }

    public Guid? SetByUserId { get; set; }

    public User? SetByUser { get; set; }

    /// <summary>True when this rate is the one that applied on the given day.</summary>
    public bool CoversDay(DateOnly day) =>
        StartDate <= day && (EndDate is null || EndDate >= day);
}
