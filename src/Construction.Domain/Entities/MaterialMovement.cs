using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One movement of stock in or out, and what it cost.
/// </summary>
/// <remarks>
/// Until now a material carried only its current quantity, which answers "how
/// much is left" and nothing else. It cannot answer what was spent, or which
/// site consumed it — the two questions the costing report exists for. A
/// running total is a summary of movements; keeping only the summary throws
/// away the thing being summarised.
///
/// <see cref="Material.Quantity"/> stays, maintained by the handler that
/// records a movement. It is a cache of the sum, kept because "what is on the
/// shelf" is read on every stock screen and summing the whole history to draw
/// a list would be paid for on every page.
/// </remarks>
public class MaterialMovement : BaseEntity
{
    public Guid MaterialId { get; set; }

    public Material Material { get; set; } = null!;

    public MaterialMovementKind Kind { get; set; }

    /// <summary>
    /// Positive for a delivery or an issue; signed for a correction.
    /// </summary>
    /// <remarks>
    /// A delivery and an issue already carry their direction in
    /// <see cref="Kind"/>, and letting them be negative would mean a negative
    /// delivery quietly increases stock. A correction is different: it is a
    /// signed delta by nature — the shelf was counted and there is more or
    /// less than the books said — and forcing it positive would need two
    /// kinds meaning the same thing in opposite directions.
    /// </remarks>
    public decimal Quantity { get; set; }

    /// <summary>
    /// What a unit was worth at the moment of this movement.
    /// </summary>
    /// <remarks>
    /// On a delivery it is what was paid. On an issue it is a snapshot of the
    /// weighted average of what has been bought so far — snapshotted, not
    /// derived at report time, so that a delivery arriving next month at a
    /// different price cannot change what last month's job is recorded as
    /// having cost.
    ///
    /// Null on a correction: stock that went missing was not consumed by any
    /// site, and pricing it would put an unexplained loss into somebody's
    /// project total.
    /// </remarks>
    public decimal? UnitPrice { get; set; }

    /// <summary>Which site consumed it. Null for a delivery into the store.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public DateOnly OccurredOn { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }

    /// <summary>How this movement changes the quantity on the shelf.</summary>
    public decimal SignedQuantity => Kind switch
    {
        MaterialMovementKind.Out => -Quantity,
        // In is positive by constraint; a correction is already signed.
        _ => Quantity
    };

    /// <summary>
    /// What this movement was worth, or null when no price applies.
    /// </summary>
    /// <remarks>
    /// Absolute, because a correction's quantity can be negative and a
    /// negative cost is not a thing anyone wants summed into a report.
    /// </remarks>
    public decimal? TotalCost =>
        UnitPrice is { } price ? price * Math.Abs(Quantity) : null;
}
