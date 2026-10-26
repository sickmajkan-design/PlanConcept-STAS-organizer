using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One line of a kit's history: who reported what, what the office decided, what was charged or refunded. The item's
/// name is copied in, so the line still reads right after the item itself is removed from the kit.
/// </summary>
public class ToolKitEvent : BaseEntity, IAuditable
{
    public Guid ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    /// <summary>The item it is about. No relation: the line outlives the item.</summary>
    public Guid? ItemId { get; set; }

    public string ItemName { get; set; } = null!;

    public ToolKitEventType Type { get; set; }

    /// <summary>
    /// Money tied to the line: what the worker is charged (<see cref="ToolKitEventType.ChargedToWorker"/>) or what
    /// the company owes them (<see cref="ToolKitEventType.ReplacementBought"/>).
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>The worker the charge or the refund is about.</summary>
    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string? Note { get; set; }

    /// <summary>For a refund: with the pay or in cash.</summary>
    public ToolKitPayoutMethod? PayoutMethod { get; set; }

    /// <summary>When the charge was collected or the refund paid out. Null while it is still open.</summary>
    public DateTime? SettledAt { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
