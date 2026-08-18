namespace Construction.Application.Features.Absences.Models;

/// <summary>
/// An employee's annual leave standing for one calendar year.
/// </summary>
public class AbsenceBalanceDto
{
    public Guid EmployeeId { get; init; }

    public int Year { get; init; }

    public int AllowanceDays { get; init; }

    /// <summary>Approved annual leave days already taken this year, clipped to the year.</summary>
    public int UsedDays { get; init; }

    /// <summary>
    /// Can go negative: an allowance lowered mid-year, or leave granted ahead
    /// of an update to it, both show up here rather than being hidden.
    /// </summary>
    public int RemainingDays => AllowanceDays - UsedDays;
}
