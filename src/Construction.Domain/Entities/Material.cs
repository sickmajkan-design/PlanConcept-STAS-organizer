using Construction.Domain.Common;

namespace Construction.Domain.Entities;

public class Material : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;

    /// <summary>Unit of measure, e.g. kg, m³, pcs.</summary>
    public string Unit { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string? Warehouse { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>Timestamp of the last inventory movement (set on every quantity change).</summary>
    public DateTime LastUpdated { get; set; }

    public ICollection<MaterialMovement> Movements { get; set; } = new List<MaterialMovement>();

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
