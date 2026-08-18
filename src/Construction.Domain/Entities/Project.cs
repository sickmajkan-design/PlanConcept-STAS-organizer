using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Project : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Client { get; set; }

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<EmployeeProject> EmployeeAssignments { get; set; } = new List<EmployeeProject>();

    public ICollection<Tool> AssignedTools { get; set; } = new List<Tool>();

    public ICollection<Material> Materials { get; set; } = new List<Material>();

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();

    public ICollection<MaterialMovement> MaterialMovements { get; set; } = new List<MaterialMovement>();

    public ICollection<FinanceEntry> FinanceEntries { get; set; } = new List<FinanceEntry>();
}
