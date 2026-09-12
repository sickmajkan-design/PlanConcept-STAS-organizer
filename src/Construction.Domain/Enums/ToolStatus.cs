namespace Construction.Domain.Enums;

public enum ToolStatus
{
    Available = 1,
    Assigned = 2,
    UnderRepair = 3,
    Lost = 4,
    Retired = 5,

    /// <summary>Out with another company — see <see cref="Entities.ToolRentalOut"/>.</summary>
    RentedOut = 6
}
