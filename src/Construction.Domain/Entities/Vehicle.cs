using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Vehicle : BaseEntity, ISoftDeletable
{
    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    public string? Vin { get; set; }

    public FuelType FuelType { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    public Guid? AssignedEmployeeId { get; set; }

    public Employee? AssignedEmployee { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<VehicleExpense> Expenses { get; set; } = new List<VehicleExpense>();

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
