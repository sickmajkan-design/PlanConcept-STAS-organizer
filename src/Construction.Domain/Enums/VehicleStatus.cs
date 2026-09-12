namespace Construction.Domain.Enums;

public enum VehicleStatus
{
    Available = 1,
    Assigned = 2,
    InService = 3,
    OutOfService = 4,

    /// <summary>Out with another company — see <see cref="Entities.VehicleRentalOut"/>.</summary>
    RentedOut = 5
}
