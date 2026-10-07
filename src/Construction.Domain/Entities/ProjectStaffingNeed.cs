using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// How many people of one position a project needs for as long as it runs.
/// </summary>
/// <remarks>
/// One number per position for the whole project, on purpose: the planning screen compares it with
/// who is posted on each day, and a need that changes week by week would be a second schedule to
/// keep in step with the first. The position is the same free text an employee carries in
/// <see cref="Employee.Position"/>; two are the same position when they match ignoring case and
/// surrounding spaces.
/// </remarks>
public class ProjectStaffingNeed : BaseEntity, IAuditable
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public string Position { get; set; } = null!;

    public int Count { get; set; }
}
