using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One stretch of time this tool went out to another company. See
/// <see cref="VehicleRentalOut"/> for the full rationale — same shape, same
/// rules, for a tool instead of a vehicle.
/// </summary>
public class ToolRentalOut : BaseEntity, IAuditable
{
    public Guid ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public string RenterName { get; set; } = null!;

    public decimal DailyRate { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Null means the tool has not come back yet.</summary>
    public DateOnly? EndDate { get; set; }

    public string? Note { get; set; }

    public Guid? SetByUserId { get; set; }

    public User? SetByUser { get; set; }
}
