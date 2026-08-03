namespace Construction.Domain.Enums;

public enum WorkItemStatus
{
    Open = 1,
    InProgress = 2,

    /// <summary>The work is done and waiting to be checked.</summary>
    Resolved = 3,

    /// <summary>Checked and finished. Terminal.</summary>
    Closed = 4,

    /// <summary>Dropped without being done. Terminal.</summary>
    Cancelled = 5
}
