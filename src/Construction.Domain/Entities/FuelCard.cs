using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A fuel card issued against one vehicle for its working life.
/// </summary>
/// <remarks>
/// Not a dated chain like <see cref="VehicleRentalRate"/> — a card is handed
/// out once and stays with the vehicle until it is lost or replaced, closer in
/// spirit to <see cref="Vehicle.GpsProvider"/>. A vehicle can still end up with
/// more than one card over its life (a lost card gets a replacement), so this
/// hangs off <see cref="Vehicle.FuelCards"/> as a collection rather than a
/// single reference, with the card number itself carrying the join used by the
/// statement import.
/// </remarks>
public class FuelCard : BaseEntity, ISoftDeletable, IAuditable
{
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    /// <summary>Free text rather than an enum — DKV is not the only issuer this company deals with regionally.</summary>
    public string Provider { get; set; } = null!;

    /// <summary>
    /// The join key a monthly statement import matches against. Unique among
    /// non-deleted cards, never across all time — a retired card's number can
    /// be reused once it is soft-deleted, since providers do reissue numbers.
    /// </summary>
    public string CardNumber { get; set; } = null!;

    public DateOnly? IssuedOn { get; set; }

    public string? Note { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
