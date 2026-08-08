using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Money spent on a vehicle: fuel, servicing, repairs, insurance.
/// </summary>
/// <remarks>
/// One table rather than a fuel log beside a service book, because they
/// differ in two nullable fields and agree on everything else — date, amount,
/// odometer, supplier, note. Splitting them would double the queries, the
/// screens and the export for the sake of <see cref="Litres"/>, and the
/// question being asked ("what has this van cost us this year") wants them
/// added together anyway.
///
/// This is the same reasoning that put tasks and defects in one table, and
/// the opposite of the attachment owners, which were kept apart because a
/// discriminator there would have given up foreign keys. Here there is one
/// owner and nothing to give up.
/// </remarks>
public class VehicleExpense : BaseEntity, IAuditable
{
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public VehicleExpenseKind Kind { get; set; }

    /// <summary>What it cost, in the system's single currency.</summary>
    public decimal Amount { get; set; }

    public DateOnly OccurredOn { get; set; }

    /// <summary>Litres filled. Only ever set for <see cref="VehicleExpenseKind.Fuel"/>.</summary>
    public decimal? Litres { get; set; }

    /// <summary>
    /// The odometer at the time, when whoever recorded it bothered to look.
    /// </summary>
    /// <remarks>
    /// Optional on purpose. Requiring it would mean a driver who forgot the
    /// reading either does not record the fuel at all or invents a number,
    /// and an invented odometer is worse than a missing one.
    /// </remarks>
    public int? OdometerKm { get; set; }

    public string? Supplier { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }

    /// <summary>
    /// Cost per litre, when both numbers are known. Derived rather than
    /// stored, so it can never disagree with the two it comes from.
    /// </summary>
    public decimal? PricePerLitre =>
        Litres is { } litres && litres > 0 ? Amount / litres : null;
}
