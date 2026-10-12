namespace Construction.Domain.Enums;

/// <summary>Where a category lands in the "from hours to profit" overview and in the monthly settlement.</summary>
public enum BookCategoryBucket
{
    /// <summary>What clients are billed.</summary>
    Collection = 1,

    /// <summary>What workers earn.</summary>
    WorkerEarnings = 2,

    /// <summary>Costs that belong to a site.</summary>
    SiteCosts = 3,

    /// <summary>Costs of the company itself, regardless of sites.</summary>
    Fixed = 4,
}
