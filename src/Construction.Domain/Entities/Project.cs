using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Project : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    /// <summary>
    /// Set when this project is a sub-project of a Main project. Null means
    /// this project is itself a Main project. A project whose parent has a
    /// parent of its own is never created — hierarchy is only ever two levels
    /// deep, enforced in <c>CreateProjectCommand</c>/<c>UpdateProjectCommand</c>.
    /// </summary>
    public Guid? ParentProjectId { get; set; }

    public Project? ParentProject { get; set; }

    public ICollection<Project> SubProjects { get; set; } = new List<Project>();

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    /// <summary>
    /// The site's expected daily clock-in time, in UTC, when one is set.
    /// Used only to flag a time entry as clocked in outside the expected
    /// window — it is not a schedule and does not gate clock-in itself.
    /// </summary>
    public TimeOnly? ShiftStartTime { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    /// <summary>The total agreed value of the contract, in the system's single currency.</summary>
    public decimal? ContractValue { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<EmployeeProject> EmployeeAssignments { get; set; } = new List<EmployeeProject>();

    public ICollection<Tool> AssignedTools { get; set; } = new List<Tool>();

    public ICollection<Vehicle> AssignedVehicles { get; set; } = new List<Vehicle>();

    public ICollection<Material> Materials { get; set; } = new List<Material>();

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();

    public ICollection<MaterialMovement> MaterialMovements { get; set; } = new List<MaterialMovement>();

    public ICollection<FinanceEntry> FinanceEntries { get; set; } = new List<FinanceEntry>();

    public ICollection<ProjectRevenue> Revenues { get; set; } = new List<ProjectRevenue>();
}
