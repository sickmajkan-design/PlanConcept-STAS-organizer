using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Tool : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public string? SerialNumber { get; set; }

    /// <summary>Value encoded in the QR label attached to the physical tool.</summary>
    public string? QrCode { get; set; }

    public ToolStatus Status { get; set; } = ToolStatus.Available;

    public Guid? AssignedEmployeeId { get; set; }

    public Employee? AssignedEmployee { get; set; }

    public Guid? AssignedProjectId { get; set; }

    public Project? AssignedProject { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
