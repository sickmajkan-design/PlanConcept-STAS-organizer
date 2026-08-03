namespace Construction.Domain.Enums;

public enum AbsenceStatus
{
    /// <summary>Asked for, and not yet answered.</summary>
    Requested = 1,

    /// <summary>Granted. Only an approved absence makes someone unavailable.</summary>
    Approved = 2,

    Rejected = 3,

    /// <summary>Withdrawn by whoever asked, before it was answered.</summary>
    Cancelled = 4
}
