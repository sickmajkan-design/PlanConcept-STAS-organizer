using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Employee : BaseEntity, ISoftDeletable, IAuditable
{
    public string EmployeeNumber { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public DateOnly EmploymentDate { get; set; }

    public string Position { get; set; } = null!;

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public User? User { get; set; }

    public ICollection<EmployeeProject> ProjectAssignments { get; set; } = new List<EmployeeProject>();

    public ICollection<Vehicle> AssignedVehicles { get; set; } = new List<Vehicle>();

    public ICollection<Tool> AssignedTools { get; set; } = new List<Tool>();

    public ICollection<LocationRecord> LocationRecords { get; set; } = new List<LocationRecord>();

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();

    public ICollection<Absence> Absences { get; set; } = new List<Absence>();

    public ICollection<EmployeeRate> Rates { get; set; } = new List<EmployeeRate>();
}
