namespace Construction.Domain.Enums;

/// <summary>What a project's spending is measured against when the office asks to be warned.</summary>
public enum BudgetAlertBasis
{
    /// <summary>The planned spending the office set for the site.</summary>
    Budget = 1,

    /// <summary>The value of the contract with the customer.</summary>
    Contract = 2,
}
