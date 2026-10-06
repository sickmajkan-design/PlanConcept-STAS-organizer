namespace Construction.Domain.Enums;

/// <summary>Which date of a vehicle a reminder is about.</summary>
public enum VehicleDateKind
{
    /// <summary>The registration runs out.</summary>
    Registration = 1,

    /// <summary>The rental or lease the company holds on the vehicle ends.</summary>
    RentedUntil = 2,

    /// <summary>A vehicle the company rented out is due back.</summary>
    RentalOutReturn = 3,

    /// <summary>The technical inspection runs out.</summary>
    TechnicalInspection = 4,

    /// <summary>The insurance runs out.</summary>
    Insurance = 5,

    /// <summary>The next service is due.</summary>
    Service = 6
}
