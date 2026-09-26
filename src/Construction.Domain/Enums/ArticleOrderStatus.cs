namespace Construction.Domain.Enums;

/// <summary>Where a request for articles stands, from asked for to in the requester's hands.</summary>
public enum ArticleOrderStatus
{
    /// <summary>Asked for; nobody in the office has acted on it yet.</summary>
    Requested = 0,

    /// <summary>The office has ordered it.</summary>
    Ordered = 1,

    /// <summary>On its way to the person who asked.</summary>
    InDelivery = 2,

    /// <summary>The person who asked has it. Final.</summary>
    Delivered = 3,

    /// <summary>The office declined it, with a reason. Final.</summary>
    Rejected = 4,

    /// <summary>Withdrawn by the person who asked, before it was ordered. Final.</summary>
    Cancelled = 5
}
