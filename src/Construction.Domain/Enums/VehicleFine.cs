namespace Construction.Domain.Enums;

public enum VehicleFineStatus
{
    /// <summary>Not paid yet. Counts on the vehicles list and is reminded about before its due date.</summary>
    Open = 1,

    Paid = 2,

    /// <summary>An appeal is under way; nothing is reminded while it is.</summary>
    Contested = 3,
}

/// <summary>Who pays a fine in the end. A fine the driver bears is also a debt of that driver.</summary>
public enum VehicleFineBearer
{
    Firm = 1,

    Driver = 2,
}
