using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Employee : BaseEntity, ISoftDeletable
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

    /// <summary>URL (or storage key) of the employee photo.</summary>
    public string? PhotoUrl { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public User? User { get; set; }

    public ICollection<EmployeeProject> ProjectAssignments { get; set; } = new List<EmployeeProject>();

    public ICollection<Vehicle> AssignedVehicles { get; set; } = new List<Vehicle>();

    public ICollection<Tool> AssignedTools { get; set; } = new List<Tool>();

    public ICollection<LocationRecord> LocationRecords { get; set; } = new List<LocationRecord>();
}
