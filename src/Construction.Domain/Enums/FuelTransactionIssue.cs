namespace Construction.Domain.Enums;

/// <summary>What is wrong with a statement row, when anything is.</summary>
public enum FuelTransactionIssue
{
    None = 0,

    /// <summary>The card is not registered against any vehicle.</summary>
    UnknownCard = 1,

    /// <summary>The TD printed on the row is not the TD of the vehicle the card is issued to.</summary>
    TdMismatch = 2,

    /// <summary>Petrol on a diesel vehicle or the reverse.</summary>
    FuelTypeMismatch = 3,

    /// <summary>A driver entry exists for that day but for another amount.</summary>
    AmountMismatch = 4,

    /// <summary>Nobody recorded this fill-up.</summary>
    NoDriverEntry = 5,

    /// <summary>The driver's entry matches, but it has no odometer reading or no photo of the receipt.</summary>
    IncompleteEntry = 6
}
