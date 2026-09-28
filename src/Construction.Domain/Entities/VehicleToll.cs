using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// One toll obligation a vehicle carries: a country's vignette, a tunnel
/// toll, or a road-passage charge. A vehicle can carry any number of these
/// — a Slovenia vignette and an Austria vignette are two rows, not one row
/// with two countries.
/// </summary>
/// <remarks>
/// Hard-deleted like <see cref="VehicleRentalRate"/>, not soft-deleted like
/// <see cref="Attachment"/>: nothing else references a toll row by id — no
/// attachment, no cost record — so nothing is left dangling once it is
/// gone. The paid/renewed history that does matter for audit purposes lives
/// in <see cref="VehicleTollPayment"/>, which outlives this row's deletion
/// on purpose — see its own remarks.
/// </remarks>
public class VehicleToll : BaseEntity, IAuditable
{
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public VehicleTollType Type { get; set; }

    public string Country { get; set; } = null!;

    /// <summary>
    /// Free-text route/segment/tunnel name, e.g. "Tunel Learjak",
    /// "A1 Beograd-Zagreb", "cijela dionica". Optional — a vignette usually
    /// covers a whole country, not a segment.
    /// </summary>
    public string? RouteSegment { get; set; }

    public VehicleTollStatus Status { get; set; } = VehicleTollStatus.Unpaid;

    /// <summary>Set once paid; carries the current validity period. Null while <see cref="Status"/> is Unpaid.</summary>
    public DateOnly? ValidUntil { get; set; }

    public Guid? PaidByUserId { get; set; }

    public User? PaidByUser { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>Every time this toll was marked paid — initial payment and every renewal. See <see cref="VehicleTollPayment"/>.</summary>
    public ICollection<VehicleTollPayment> Payments { get; set; } = new List<VehicleTollPayment>();

    /// <summary>True once a paid toll's validity has run out.</summary>
    public bool IsExpiredOn(DateOnly today) =>
        Status == VehicleTollStatus.Paid && ValidUntil is { } until && until < today;

    /// <summary>True while still valid but running out within <paramref name="withinDays"/> days.</summary>
    public bool IsExpiringSoonOn(DateOnly today, int withinDays = 7) =>
        Status == VehicleTollStatus.Paid
        && ValidUntil is { } until
        && until >= today
        && until <= today.AddDays(withinDays);
}
