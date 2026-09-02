using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// What this employee's time costs, over a stretch of time — per hour, or
/// per day worked.
/// </summary>
/// <remarks>
/// A rate is dated rather than a column on <see cref="Employee"/> because a
/// cost report is about the past. Everyone got a raise in June; the March
/// report must still say what March cost. A single current-rate column would
/// silently rewrite every report that was ever run whenever anyone's pay
/// changed — and the number would keep looking plausible, which is what makes
/// it dangerous.
///
/// It is the cost to the company, not the wage: the useful number for
/// pricing a job is what the hour or day costs once contributions are
/// included. What goes into it is the office's decision; the model only
/// stores it.
///
/// Two shapes share one row rather than two tables, because everything else
/// about a rate — who it belongs to, the dates it covers, the office's note,
/// who set it — is identical either way, and <see cref="RateType"/> alone
/// says which of <see cref="HourlyRate"/>/<see cref="DailyRate"/> applies.
/// </remarks>
public class EmployeeRate : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>Which of <see cref="HourlyRate"/>/<see cref="DailyRate"/> is in play.</summary>
    public RateType RateType { get; set; } = RateType.Hourly;

    /// <summary>
    /// Cost per hour, in the system's single currency. Set when
    /// <see cref="RateType"/> is <see cref="Enums.RateType.Hourly"/>; null
    /// otherwise.
    /// </summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>
    /// Cost per hour on a Saturday or Sunday. Null means no premium — a
    /// weekend hour costs the same as any other. Only meaningful alongside
    /// <see cref="HourlyRate"/> — a daily rate has no weekend variant.
    /// </summary>
    public decimal? WeekendHourlyRate { get; set; }

    /// <summary>
    /// Cost per hour on a day listed in <see cref="PublicHoliday"/>. Null
    /// means no premium. Only meaningful alongside <see cref="HourlyRate"/>.
    /// </summary>
    public decimal? HolidayHourlyRate { get; set; }

    /// <summary>
    /// One flat amount per day worked. Set when <see cref="RateType"/> is
    /// <see cref="Enums.RateType.Daily"/>; null otherwise. The usual shape
    /// for a subcontractor's rate.
    /// </summary>
    public decimal? DailyRate { get; set; }

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
