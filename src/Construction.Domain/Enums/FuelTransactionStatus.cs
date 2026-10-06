namespace Construction.Domain.Enums;

/// <summary>Where a DKV statement row stands against what the drivers recorded.</summary>
public enum FuelTransactionStatus
{
    /// <summary>Paired with a driver's fuel entry; nothing for anyone to do.</summary>
    Matched = 1,

    /// <summary>A driver's entry exists but something about the row disagrees with it or with the vehicle.</summary>
    NeedsReview = 2,

    /// <summary>The card is known and the row is clean, but no driver has recorded this fill-up (yet).</summary>
    NoDriverEntry = 3,

    /// <summary>The card number belongs to no vehicle.</summary>
    UnknownCard = 4,

    /// <summary>An office user settled it by hand — see the resolution fields.</summary>
    Resolved = 5,

    /// <summary>An office user set it aside on purpose.</summary>
    Ignored = 6
}
