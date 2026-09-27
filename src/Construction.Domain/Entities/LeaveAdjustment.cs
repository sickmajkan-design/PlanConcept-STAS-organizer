using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A manual correction of somebody's annual leave for one year: days brought over from
/// before the system, days granted as a favour, days taken off by mistake.
/// </summary>
/// <remarks>
/// Positive adds to the year's right, negative takes from it, and what is left of it at the
/// end of the year carries over like any other day. Never edited once written: a wrong one
/// is answered with a new one that undoes it, so the history stays honest.
/// </remarks>
public class LeaveAdjustment : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>The calendar year the correction belongs to.</summary>
    public int Year { get; set; }

    /// <summary>Whole working days; never zero.</summary>
    public int Days { get; set; }

    /// <summary>Why. Required: it is what makes a correction defensible later.</summary>
    public string Reason { get; set; } = null!;

    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }
}
