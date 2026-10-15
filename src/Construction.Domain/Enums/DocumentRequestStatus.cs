namespace Construction.Domain.Enums;

/// <summary>Where a request for a document stands.</summary>
public enum DocumentRequestStatus
{
    /// <summary>Waiting for the person to send the document.</summary>
    Open = 1,

    /// <summary>Sent; waiting for the office to check it.</summary>
    Submitted = 2,

    /// <summary>Checked and accepted; the document is on the person's record and shared with them.</summary>
    Accepted = 3,

    /// <summary>Checked and sent back with a reason; the person sends a new one.</summary>
    Returned = 4,

    /// <summary>Withdrawn by the office.</summary>
    Cancelled = 5
}

/// <summary>One step in the life of a request, kept so both sides can see what happened and when.</summary>
public enum DocumentRequestEventType
{
    Created = 1,
    Submitted = 2,
    Accepted = 3,
    Returned = 4,
    Cancelled = 5
}
