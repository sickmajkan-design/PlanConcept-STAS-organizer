namespace Construction.Domain.Enums;

/// <summary>Where a recorded vehicle cost is in the approval chain.</summary>
public enum VehicleExpenseStatus
{
    /// <summary>Recorded, not yet reviewed.</summary>
    Pending = 1,

    /// <summary>Signed off.</summary>
    Approved = 2,

    /// <summary>Sent back with a reason. Editing it returns it to <see cref="Pending"/>.</summary>
    Rejected = 3
}
