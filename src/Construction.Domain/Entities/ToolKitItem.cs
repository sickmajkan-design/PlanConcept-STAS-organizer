using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One line of what a tool kit (a <see cref="Tool"/> with <see cref="Tool.IsKit"/>) is made of: a pair of scissors,
/// two screwdrivers. The price is per piece, so what is missing from a case is worth something specific.
/// </summary>
public class ToolKitItem : BaseEntity, IAuditable
{
    public Guid ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    /// <summary>What one piece cost, in the system's single currency.</summary>
    public decimal UnitPrice { get; set; }

    public ToolKitItemState State { get; set; } = ToolKitItemState.InKit;

    public int SortOrder { get; set; }
}
