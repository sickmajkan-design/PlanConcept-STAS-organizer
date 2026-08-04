namespace Construction.Domain.Enums;

public enum MaterialMovementKind
{
    /// <summary>A delivery into the store. Carries a purchase price.</summary>
    In = 1,

    /// <summary>Issued to a site and consumed.</summary>
    Out = 2,

    /// <summary>
    /// A stocktake correction: the shelf was counted and did not match.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="In"/> and <see cref="Out"/> so breakage and
    /// miscounting do not masquerade as purchases or as consumption on a
    /// site — that would put losses into a project's cost.
    /// </remarks>
    Adjustment = 3
}
