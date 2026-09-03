using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// What housing a worker in a given apartment costs the company per month,
/// over a stretch of time.
/// </summary>
/// <remarks>
/// Dated the same way as <see cref="VehicleRentalRate"/>, and for the same
/// reason: a cost report is about the past, and rent renewed in June must
/// not rewrite what March's report said housing cost.
/// </remarks>
public class AccommodationRate : BaseEntity, IAuditable
{
    public Guid AccommodationId { get; set; }

    public Accommodation Accommodation { get; set; } = null!;

    /// <summary>What the landlord/agency gets per month, in the system's single currency.</summary>
    public decimal MonthlyAmount { get; set; }

    /// <summary>Who the apartment is rented from.</summary>
    public string? Provider { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null means it is the rate in force, with no end set.</summary>
    public DateOnly? EndDate { get; set; }

    public string? Note { get; set; }

    public Guid? SetByUserId { get; set; }

    public User? SetByUser { get; set; }

    /// <summary>True when this rate is the one that applied on the given day.</summary>
    public bool CoversDay(DateOnly day) =>
        StartDate <= day && (EndDate is null || EndDate >= day);
}
