namespace Construction.Application.Features.Branches;

/// <summary>
/// Which business unit a figure about people's work is counted under when a unit is asked for.
/// </summary>
/// <remarks>
/// A person employed by one unit can work on a site of another. Hours and pay then have two
/// owners that are both right: the unit whose site it was (where the work is billed) and the unit
/// that employs them (which pays them). Neither is a mistake, so reports let the reader choose.
/// Everything that is not about people — materials, vehicles, tools, other costs, revenue —
/// belongs to its own unit whichever is chosen.
/// </remarks>
public enum BranchBasis
{
    /// <summary>The unit whose site the work was done on.</summary>
    Site = 0,

    /// <summary>The unit that employed the person on the day.</summary>
    Employer = 1,
}
