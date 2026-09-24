namespace Construction.Domain.Enums;

/// <summary>
/// How much of the company's money an account may see. Granted per account by
/// a SuperAdmin, on the same footing as <c>User.CanViewCustomerTaxDetails</c>;
/// a SuperAdmin always has <see cref="Full"/>.
/// </summary>
public enum FinanceAccess
{
    /// <summary>No income, spending or profit figures — the default.</summary>
    None = 0,

    /// <summary>
    /// Trends, percentage changes and shares derived from the figures, never
    /// the amounts themselves.
    /// </summary>
    StatisticsOnly = 1,

    /// <summary>The amounts, and the pages they come from.</summary>
    Full = 2,
}
