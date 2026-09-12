using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// What a rented or leased tool costs the company per month, over a stretch
/// of time. Mirrors <see cref="VehicleRentalRate"/> for the same reason: a
/// cost report is about the past, and a rate renewed in June must not
/// rewrite what March's tool report said the compressor cost.
/// </summary>
public class ToolRentalRate : BaseEntity, IAuditable
{
    public Guid ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    /// <summary>What the rental/lease company gets per month, in the system's single currency.</summary>
    public decimal MonthlyAmount { get; set; }

    /// <summary>Who the tool is rented or leased from.</summary>
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
