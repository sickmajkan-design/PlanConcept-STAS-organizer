namespace Construction.Domain.Enums;

/// <summary>
/// Who is on the crew, not how well they're doing — <see cref="EmployeeStatus"/>
/// already covers that. A subcontractor goes through the same roster,
/// scheduling, and time-tracking as a direct employee; only their pay is
/// typically structured differently (see <see cref="RateType"/>).
/// </summary>
public enum EmployeeType
{
    Employee = 1,
    Subcontractor = 2
}
