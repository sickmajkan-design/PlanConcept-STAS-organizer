namespace Construction.Domain.Enums;

/// <summary>How an <see cref="Entities.EmployeeRate"/> prices a shift.</summary>
public enum RateType
{
    /// <summary>Cost per hour worked, priced from <see cref="Entities.EmployeeRate.HourlyRate"/>.</summary>
    Hourly = 1,

    /// <summary>
    /// One flat amount per day worked, regardless of hours — the usual way a
    /// subcontractor is billed. Priced from
    /// <see cref="Entities.EmployeeRate.DailyRate"/>; a day is "worked" when
    /// there is at least one approved, clocked-out time entry on it.
    /// </summary>
    Daily = 2
}
