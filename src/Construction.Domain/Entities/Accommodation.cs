using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A rented apartment or house the company houses workers in — the asset
/// itself; what it costs per month lives in <see cref="AccommodationRate"/>.
/// </summary>
public class Accommodation : BaseEntity, ISoftDeletable, IAuditable
{
    public string Address { get; set; } = null!;

    /// <summary>What people call it: "Stan 4, Vidikovac". Falls back to the address when empty.</summary>
    public string? Name { get; set; }

    public AccommodationType Type { get; set; } = AccommodationType.Apartment;

    public string? City { get; set; }

    /// <summary>Free text ("3", "prizemlje"), because a floor is not always a number.</summary>
    public string? Floor { get; set; }

    public int? Rooms { get; set; }

    /// <summary>How many people can sleep there. Occupancy is measured against it.</summary>
    public int? Beds { get; set; }

    public decimal? AreaSquareMeters { get; set; }

    public string? LandlordName { get; set; }

    public string? LandlordPhone { get; set; }

    public string? LandlordEmail { get; set; }

    public string? ContractNumber { get; set; }

    public DateOnly? ContractStart { get; set; }

    public DateOnly? ContractEnd { get; set; }

    /// <summary>The contract end date the expiry warning was already sent for; a changed end date is a new warning.</summary>
    public DateOnly? ContractExpiryNotifiedFor { get; set; }

    /// <summary>The deposit paid to the landlord, for reference. The payment itself is a one-off charge.</summary>
    public decimal? DepositAmount { get; set; }

    /// <summary>Whether utilities (electricity, water, heating) are already in the rent.</summary>
    public bool UtilitiesIncluded { get; set; }

    /// <summary>False once the firm no longer rents it. Kept for the history.</summary>
    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<AccommodationRate> Rates { get; set; } = new List<AccommodationRate>();

    public ICollection<AccommodationStay> Stays { get; set; } = new List<AccommodationStay>();
}
