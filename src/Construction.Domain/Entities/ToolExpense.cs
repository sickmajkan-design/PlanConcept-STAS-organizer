using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Money spent on a tool: a repair, a service, recalibrating an instrument.
/// </summary>
/// <remarks>
/// Mirrors <see cref="VehicleExpense"/> without the two fields specific to a
/// vehicle — a drill has no litres and no odometer. Kept separate from the
/// project cost report for the same reason a vehicle is: a tool is not
/// "posted" to a site the way a person is, so attributing its repair to one
/// project would mean inventing an allocation the data does not support.
/// </remarks>
public class ToolExpense : BaseEntity, IAuditable
{
    public Guid ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    public ToolExpenseKind Kind { get; set; }

    /// <summary>What it cost, in the system's single currency.</summary>
    public decimal Amount { get; set; }

    public DateOnly OccurredOn { get; set; }

    public string? Supplier { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
