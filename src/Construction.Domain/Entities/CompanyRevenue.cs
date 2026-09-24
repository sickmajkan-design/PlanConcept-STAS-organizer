using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Money the company actually received that belongs to no project — renting a
/// vehicle or a tool out, or anything else.
/// </summary>
/// <remarks>
/// The other half of <see cref="ProjectRevenue"/>: what comes in against a
/// contract is that entity's, everything else is this one. The company's
/// income for a period is the two added together. A rental may name the
/// vehicle or tool it came from, so the asset's own page can later show
/// whether it earns its keep.
/// </remarks>
public class CompanyRevenue : BaseEntity, IAuditable
{
    /// <summary>What was received, in the system's single currency.</summary>
    public decimal Amount { get; set; }

    public DateOnly OccurredOn { get; set; }

    public CompanyRevenueSource Source { get; set; }

    /// <summary>Only for <see cref="CompanyRevenueSource.VehicleRental"/>, and optional even then.</summary>
    public Guid? VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    /// <summary>Only for <see cref="CompanyRevenueSource.ToolRental"/>, and optional even then.</summary>
    public Guid? ToolId { get; set; }

    public Tool? Tool { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
