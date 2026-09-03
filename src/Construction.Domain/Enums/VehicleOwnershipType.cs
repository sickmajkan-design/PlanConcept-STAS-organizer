namespace Construction.Domain.Enums;

/// <summary>Whether the company owns a vehicle outright, or pays for the use of it.</summary>
public enum VehicleOwnershipType
{
    Owned = 1,

    /// <summary>Short-term hire — a rent-a-car arrangement.</summary>
    Rented = 2,

    /// <summary>A longer-term leasing contract.</summary>
    Leased = 3
}
