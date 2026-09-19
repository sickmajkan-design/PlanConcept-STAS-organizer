using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One person living in one accommodation for a stretch of days.
/// </summary>
/// <remarks>
/// Occupancy is a fact with dates, not a field on the employee, for the same
/// reason a posting to a site has dates: people move, and what the firm paid
/// for last month's rooms depends on who was in them last month. A person can
/// be in only one place at a time; the database refuses overlapping stays.
/// </remarks>
public class AccommodationStay : BaseEntity, IAuditable
{
    public Guid AccommodationId { get; set; }

    public Accommodation Accommodation { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>First night.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Last day counted. Null while they still live there.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// The project the housing is charged to, when it is a different answer from
    /// "wherever they work". Null leaves it uncharged to any project.
    /// </summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public string? Note { get; set; }

    public bool CoversDay(DateOnly day) =>
        StartDate <= day && (EndDate is null || EndDate >= day);
}
