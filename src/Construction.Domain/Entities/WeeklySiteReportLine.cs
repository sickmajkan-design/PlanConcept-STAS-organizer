using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// What one worker did on the site in the report's week, as the foreman wrote it down and as the office
/// checked it against the picture. When approved, these lines are what is entered into the month's sheet.
/// </summary>
public class WeeklySiteReportLine : BaseEntity
{
    public Guid ReportId { get; set; }

    public WeeklySiteReport Report { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>Hours for the whole calendar week.</summary>
    public decimal Hours { get; set; }

    /// <summary>
    /// How many of <see cref="Hours"/> fall in the later month, when the week runs over a month's end (its Monday
    /// is in one month and its Sunday in the next). Zero for every other week.
    /// </summary>
    public decimal HoursSecondMonth { get; set; }

    public int SortOrder { get; set; }
}
