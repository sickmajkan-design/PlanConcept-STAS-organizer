namespace Construction.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between employees and projects.
/// Modelled explicitly so the assignment itself carries data (when / by whom).
/// </summary>
public class EmployeeProject
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public Guid? AssignedByUserId { get; set; }
}
