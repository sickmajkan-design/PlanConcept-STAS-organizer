namespace Construction.Domain.Enums;

/// <summary>What a book entry does to the company's money. The amount is always positive; the kind gives the direction.</summary>
public enum BookEntryKind
{
    /// <summary>Money earned and billed: raises income and open receivables.</summary>
    Income = 1,

    /// <summary>Money spent: lowers profit, and is owed until it is paid.</summary>
    Cost = 2,

    /// <summary>A client paid: lowers open receivables, changes no profit.</summary>
    Payment = 3,

    /// <summary>The company paid out money that is already counted as a cost (a wage): changes no profit.</summary>
    Payout = 4,
}
