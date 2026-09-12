using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>One line within a <see cref="LedgerSection"/> — usually a worker.</summary>
public class LedgerRow : BaseEntity, IAuditable
{
    public Guid SectionId { get; set; }

    public LedgerSection Section { get; set; } = null!;

    public string Label { get; set; } = null!;

    /// <summary>Optional cross-reference to a real employee. Free text otherwise.</summary>
    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    /// <summary>
    /// Optional cross-reference to a real vehicle — what a
    /// <see cref="Enums.LedgerColumnSourceMetric.VehicleTotalCost"/> column
    /// reads its value for on this row. At most one of
    /// <see cref="EmployeeId"/>/<see cref="VehicleId"/>/<see cref="ToolId"/>/
    /// <see cref="MaterialId"/> is set — enforced at the application layer,
    /// not the database.
    /// </summary>
    public Guid? VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    /// <summary>Optional cross-reference to a real tool — see <see cref="VehicleId"/>.</summary>
    public Guid? ToolId { get; set; }

    public Tool? Tool { get; set; }

    /// <summary>Optional cross-reference to a real material — see <see cref="VehicleId"/>.</summary>
    public Guid? MaterialId { get; set; }

    public Material? Material { get; set; }

    /// <summary>
    /// Set once this row has been pushed through the real General Expense
    /// form — blocks re-promoting the same row and lets the UI link to what
    /// was created. At most one of this and
    /// <see cref="PromotedAccommodationRateId"/> is set.
    /// </summary>
    public Guid? PromotedGeneralExpenseId { get; set; }

    public GeneralExpense? PromotedGeneralExpense { get; set; }

    /// <summary>Set once this row has been pushed through the real Accommodation-rate form — see <see cref="PromotedGeneralExpenseId"/>.</summary>
    public Guid? PromotedAccommodationRateId { get; set; }

    public AccommodationRate? PromotedAccommodationRate { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Hex color for the row's background (e.g. "#F6C6C6"), or null for none.</summary>
    public string? ColorTag { get; set; }

    public ICollection<LedgerCell> Cells { get; set; } = new List<LedgerCell>();
}
