namespace Construction.Application.Features.Absences.Models;

/// <summary>
/// An employee's annual leave standing for one calendar year, in working days.
/// </summary>
public class AbsenceBalanceDto
{
    public Guid EmployeeId { get; init; }

    public int Year { get; init; }

    /// <summary>
    /// Everything that may be used this year as of today: the year's right (pro rata in the
    /// year of starting), corrections, and carried-over days that have not expired yet.
    /// </summary>
    public int AllowanceDays { get; init; }

    /// <summary>Approved annual leave working days in the year, booked ahead included.</summary>
    public int UsedDays { get; init; }

    /// <summary>
    /// Can go negative: leave granted beyond the right shows up here rather than being hidden.
    /// </summary>
    public int RemainingDays => AllowanceDays - UsedDays;

    /// <summary>The year's own right, before carry-over and corrections.</summary>
    public int EntitlementDays { get; init; }

    /// <summary>Manual corrections for the year, positive or negative.</summary>
    public int AdjustmentDays { get; init; }

    /// <summary>Unused days brought in from last year.</summary>
    public int CarriedOverDays { get; init; }

    /// <summary>How many of the carried-over days were used before they expired.</summary>
    public int CarriedOverUsedDays { get; init; }

    /// <summary>Carried-over days already lost because 1 June has passed.</summary>
    public int CarriedOverExpiredDays { get; init; }

    /// <summary>The first day a carried-over day can no longer be used.</summary>
    public DateOnly CarryOverExpiresOn { get; init; }

    /// <summary>What would carry into next year if nothing more is taken.</summary>
    public int CarryingIntoNextYearDays { get; init; }
}
