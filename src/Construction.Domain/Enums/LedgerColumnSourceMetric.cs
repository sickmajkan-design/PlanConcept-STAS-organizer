namespace Construction.Domain.Enums;

/// <summary>
/// What a column's value is computed from, when it is not freely typed.
/// Null on <see cref="Entities.LedgerColumn.SourceMetric"/> means the
/// column is manual, today's only behavior.
/// </summary>
public enum LedgerColumnSourceMetric
{
    /// <summary>Sum of a <see cref="Entities.Vehicle"/>'s fuel/service/other/rental costs for the ledger's month — the row must link a vehicle.</summary>
    VehicleTotalCost = 1,

    /// <summary>Sum of a <see cref="Entities.Tool"/>'s repair/maintenance/other/rental costs for the ledger's month — the row must link a tool.</summary>
    ToolTotalCost = 2,

    /// <summary>Value of a <see cref="Entities.Material"/> issued out for the ledger's month (and the row's section's project, if linked) — the row must link a material.</summary>
    MaterialCost = 3,
}
