using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A day priced like a holiday, wherever an <see cref="EmployeeRate"/> sets a
/// <see cref="EmployeeRate.HolidayHourlyRate"/>.
/// </summary>
/// <remarks>
/// One row per calendar date, entered by hand — nothing recurs automatically
/// from year to year. That is a deliberate simplification: a moving public
/// holiday (tied to a lunar or religious calendar) cannot be derived from a
/// rule anyway, and a fixed one is one row a year to add.
/// </remarks>
public class PublicHoliday : BaseEntity
{
    public DateOnly Date { get; set; }

    public string Name { get; set; } = null!;
}
