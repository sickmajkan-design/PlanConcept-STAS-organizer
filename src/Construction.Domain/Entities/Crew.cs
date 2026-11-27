using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A named group of workers that is moved around together ("Ekipa A"). Only a convenience for the planner: the schedule
/// itself is still kept per person, so putting a crew on a site writes one posting per member.
/// </summary>
public class Crew : BaseEntity
{
    public string Name { get; set; } = null!;

    /// <summary>The member who leads the crew, if it has one.</summary>
    public Guid? LeaderEmployeeId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public ICollection<CrewMember> Members { get; set; } = new List<CrewMember>();
}

/// <summary>One worker in a crew. A worker belongs to at most one crew.</summary>
public class CrewMember : BaseEntity
{
    public Guid CrewId { get; set; }

    public Crew Crew { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// A day of the schedule that a planner published: everybody posted that day was sent a notice of where they go. Kept so the
/// Planner can say "published at 15:30" and how many people were told. Publishing again is allowed and adds a row.
/// </summary>
public class SchedulePublication : BaseEntity
{
    public DateOnly Date { get; set; }

    public Guid? PublishedByUserId { get; set; }

    /// <summary>How many people were sent a notice.</summary>
    public int People { get; set; }
}

/// <summary>
/// A week of the schedule saved to be put on another week ("Standardna sedmica Doboj"): who is on which site on which
/// weekday. Rows are kept as one JSON text, since a template is only ever read and written whole.
/// </summary>
public class ScheduleTemplate : BaseEntity
{
    public string Name { get; set; } = null!;

    /// <summary>A JSON array of <c>{ employeeId, weekday (0 = Monday .. 4 = Friday), projectId }</c>.</summary>
    public string Rows { get; set; } = "[]";

    public Guid? CreatedByUserId { get; set; }
}
