namespace Construction.Application.Features.Costs.Models;

/// <summary>What a set of projects cost over a period.</summary>
public class ProjectCostReportDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>
    /// False when the caller may not see pay rates. Every labour figure is
    /// then zero — omitted rather than refused, so a foreman still gets the
    /// material half of their own site's report.
    /// </summary>
    public bool IncludesLabour { get; init; }

    public IReadOnlyCollection<ProjectCostRowDto> Rows { get; init; } =
        Array.Empty<ProjectCostRowDto>();

    public decimal TotalLabourCost { get; init; }

    public decimal TotalMaterialCost { get; init; }

    /// <summary>
    /// Value of materials currently assigned to a site, priced at each
    /// material's own reference price — not part of <see cref="Total"/>. See
    /// the field of the same name on <see cref="ProjectCostRowDto"/>.
    /// </summary>
    public decimal TotalMaterialsOnSiteValue { get; init; }

    /// <summary>
    /// Manually entered pay (<c>FinanceEntry</c>) attributed to a site over
    /// the period — not part of <see cref="Total"/>. See the field of the
    /// same name on <see cref="ProjectCostRowDto"/> for why.
    /// </summary>
    public decimal TotalManualPayAmount { get; init; }

    public decimal Total { get; init; }
}

public class ProjectCostRowDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    /// <summary>Approved hours only. Unreviewed hours are not yet a cost.</summary>
    public int LabourMinutes { get; init; }

    public decimal LabourCost { get; init; }

    /// <summary>
    /// Hours that could not be priced because no rate covered the day.
    /// </summary>
    /// <remarks>
    /// Reported rather than silently treated as free. A total that quietly
    /// omits a third of the crew looks exactly like a total that does not,
    /// and the office would price the next job from it.
    /// </remarks>
    public int UnpricedMinutes { get; init; }

    public decimal MaterialCost { get; init; }

    /// <summary>
    /// Value of materials currently assigned to this site (quantity × each
    /// material's reference price), whether or not any of it has been used
    /// yet. Deliberately not folded into <see cref="MaterialCost"/> or
    /// <see cref="Total"/> — that stays what was actually issued and priced;
    /// this is what is sitting there, a different question with a different
    /// answer. Not scoped to the report's date range either: it is a
    /// snapshot of what is on site right now, not a transaction total for
    /// the period.
    /// </summary>
    public decimal MaterialsOnSiteValue { get; init; }

    /// <summary>
    /// Manually entered pay (<c>FinanceEntry</c>) attributed to this site over
    /// the period. Deliberately not folded into <see cref="LabourCost"/> or
    /// <see cref="Total"/>: a `FinanceEntry` is often a correction to hours
    /// already clocked and priced there — a rounded figure, a flat day rate —
    /// so adding the two together risks paying the same work twice on paper.
    /// Shown as its own figure instead of guessed away.
    /// </summary>
    public decimal ManualPayAmount { get; init; }

    public decimal Total { get; init; }
}

/// <summary>What the fleet cost over a period.</summary>
public class VehicleCostReportDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public IReadOnlyCollection<VehicleCostRowDto> Rows { get; init; } =
        Array.Empty<VehicleCostRowDto>();

    public decimal Total { get; init; }

    public decimal TotalFuelCost { get; init; }

    public decimal TotalLitres { get; init; }
}

public class VehicleCostRowDto
{
    public Guid VehicleId { get; init; }

    public string VehicleName { get; init; } = null!;

    public decimal FuelCost { get; init; }

    public decimal Litres { get; init; }

    public decimal ServiceCost { get; init; }

    /// <summary>Insurance, registration and everything else.</summary>
    public decimal OtherCost { get; init; }

    public decimal Total { get; init; }

    /// <summary>
    /// Distance covered between the first and last odometer reading in the
    /// period, when at least two were recorded.
    /// </summary>
    public int? DistanceKm { get; init; }

    /// <summary>
    /// Litres per 100 km, when the distance is known.
    /// </summary>
    /// <remarks>
    /// The number that actually catches a problem: a van whose consumption
    /// jumps is either broken or having its fuel card used by somebody else,
    /// and neither shows up in a total that only ever goes up.
    /// </remarks>
    public decimal? LitresPer100Km { get; init; }
}

/// <summary>What the tool fleet cost over a period.</summary>
public class ToolCostReportDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public IReadOnlyCollection<ToolCostRowDto> Rows { get; init; } =
        Array.Empty<ToolCostRowDto>();

    public decimal Total { get; init; }
}

public class ToolCostRowDto
{
    public Guid ToolId { get; init; }

    public string ToolName { get; init; } = null!;

    public decimal RepairCost { get; init; }

    public decimal MaintenanceCost { get; init; }

    /// <summary>Calibration and everything else.</summary>
    public decimal OtherCost { get; init; }

    public decimal Total { get; init; }
}
