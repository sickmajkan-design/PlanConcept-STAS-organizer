namespace Construction.Domain.Enums;

/// <summary>Where a request to be paid back stands.</summary>
public enum RefundStatus
{
    /// <summary>Asked for; nobody has decided yet.</summary>
    Requested = 0,

    /// <summary>Granted. It goes into the payroll of the month it names.</summary>
    Approved = 1,

    /// <summary>Declined, with a reason. Final.</summary>
    Rejected = 2,

    /// <summary>Withdrawn by the person who asked, before it was decided. Final.</summary>
    Cancelled = 3
}
