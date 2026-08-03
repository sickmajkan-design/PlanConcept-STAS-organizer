namespace Construction.Domain.Enums;

/// <summary>
/// Where a shift is in the approval chain. The order matters: a payroll run
/// reads <see cref="Approved"/> only, so anything else is invisible to it.
/// </summary>
public enum TimeEntryStatus
{
    /// <summary>Clocked in, not yet clocked out. There is no duration yet.</summary>
    InProgress = 1,

    /// <summary>Clocked out and awaiting review.</summary>
    Submitted = 2,

    /// <summary>Signed off. Locked against further edits.</summary>
    Approved = 3,

    /// <summary>Sent back with a reason. Editable again, and re-submitted on edit.</summary>
    Rejected = 4
}
