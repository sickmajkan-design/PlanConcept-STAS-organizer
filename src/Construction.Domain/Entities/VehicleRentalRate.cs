using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// What a rented or leased vehicle costs the company per month, over a
/// stretch of time.
/// </summary>
/// <remarks>
/// Dated the same way as <see cref="EmployeeRate"/>, and for the same
/// reason: a cost report is about the past. The rate renewed in June must
/// not rewrite what March's fleet report said the van cost. A
/// <see cref="VehicleExpense"/> cannot represent this — it is a single
/// point-in-time ledger line, and "this costs X per month starting on date
/// Y" is a period, not a point.
/// </remarks>
public class VehicleRentalRate : BaseEntity, IAuditable
{
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    /// <summary>What the rental/lease company gets per month, in the system's single currency.</summary>
    public decimal MonthlyAmount { get; set; }

    /// <summary>Who the vehicle is rented or leased from.</summary>
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
