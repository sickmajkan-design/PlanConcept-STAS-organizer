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

    /// <summary>Owned outright, rented, or leased — orthogonal to <see cref="Status"/>.</summary>
    public ToolOwnershipType OwnershipType { get; set; } = ToolOwnershipType.Owned;

    public Guid? AssignedEmployeeId { get; set; }

    public Employee? AssignedEmployee { get; set; }

    public Guid? AssignedProjectId { get; set; }

    public Project? AssignedProject { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<ToolExpense> Expenses { get; set; } = new List<ToolExpense>();

    public ICollection<ToolRentalRate> RentalRates { get; set; } = new List<ToolRentalRate>();

    /// <summary>Every time this tool went out to another company. See <see cref="ToolRentalOut"/>.</summary>
    public ICollection<ToolRentalOut> RentalsOut { get; set; } = new List<ToolRentalOut>();

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
