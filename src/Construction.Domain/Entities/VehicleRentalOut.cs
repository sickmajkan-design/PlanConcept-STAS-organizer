using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One stretch of time this vehicle went out to another company — the
/// opposite direction from <see cref="VehicleRentalRate"/>: money coming in
/// for lending the vehicle out, rather than money going out for renting one
/// in. Each row is a discrete transaction (who, from when, until when, at
/// what rate), not a dated chain the way a pay rate is — a company borrowing
/// the excavator twice in one year is two separate rows, not one rate
/// replacing another.
/// </summary>
/// <remarks>
/// A vehicle may have at most one open row (<see cref="EndDate"/> null) at a
/// time — enforced by a partial unique index — and starting one requires the
/// vehicle to be <see cref="Enums.VehicleStatus.Available"/>: equipment
/// already assigned to the company's own crew cannot also be out with
/// someone else. See <c>RecordVehicleRentalOutCommand</c>.
/// </remarks>
public class VehicleRentalOut : BaseEntity, IAuditable
{
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    /// <summary>Optional cross-reference to a tracked <see cref="Customer"/>. Free text otherwise.</summary>
    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    /// <summary>Who has the vehicle — always set, whether or not it matches a tracked customer.</summary>
    public string RenterName { get; set; } = null!;

    /// <summary>What the renter pays per day, in the system's single currency.</summary>
    public decimal DailyRate { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null means the vehicle has not come back yet.</summary>
    public DateOnly? EndDate { get; set; }

    public string? Note { get; set; }

    public Guid? SetByUserId { get; set; }

    public User? SetByUser { get; set; }
}
