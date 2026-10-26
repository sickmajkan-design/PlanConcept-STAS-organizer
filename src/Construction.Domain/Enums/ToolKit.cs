namespace Construction.Domain.Enums;

/// <summary>Where one item of a tool kit (a "kofer") stands right now.</summary>
public enum ToolKitItemState
{
    /// <summary>In the case, usable.</summary>
    InKit = 1,

    /// <summary>Not in the case and nobody has decided yet what happens.</summary>
    Missing = 2,

    /// <summary>Broken, still with the worker; it has to reach the office before anything else happens.</summary>
    Broken = 3,

    /// <summary>Broken and now with the office: repair, replace or write off.</summary>
    BrokenHandedIn = 4,

    /// <summary>Missing and its price charged to the worker. Still absent from the case until restocked.</summary>
    Charged = 5,

    /// <summary>Missing or broken and written off with no charge. Still absent until restocked.</summary>
    WrittenOff = 6,
}

/// <summary>What can be done to a kit item. Each one moves the item between <see cref="ToolKitItemState"/> values.</summary>
public enum ToolKitAction
{
    ReportMissing = 1,
    ReportBroken = 2,
    ReceiveBroken = 3,
    ChargeWorker = 4,
    WriteOff = 5,

    /// <summary>The item is back in the case: found, repaired or replaced from the stock room.</summary>
    Restore = 6,

    /// <summary>The worker bought a replacement with their own money and is owed a refund.</summary>
    ReplacementBought = 7,
}

/// <summary>What one line of a kit's history records.</summary>
public enum ToolKitEventType
{
    ItemAdded = 1,
    ItemRemoved = 2,
    ItemChanged = 3,
    ReportedMissing = 4,
    ReportedBroken = 5,
    BrokenReceived = 6,
    ChargedToWorker = 7,
    WrittenOff = 8,
    Restored = 9,
    ReplacementBought = 10,
    Settled = 11,

    /// <summary>The worker bought a replacement and is not refunded: the damage was theirs.</summary>
    ReplacementNotRefunded = 12,
}

/// <summary>How a refund owed to a worker is paid. Nothing here moves money: it tells the office where to pay it.</summary>
public enum ToolKitPayoutMethod
{
    /// <summary>Added to the worker's pay by the office when the month is settled.</summary>
    WithPayroll = 1,

    /// <summary>Handed to the worker in cash.</summary>
    Cash = 2,
}
