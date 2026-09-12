namespace Construction.Domain.Enums;

/// <summary>
/// Which export a subscription re-runs on its own schedule. Limited to the
/// period-based exports (a "last week" / "last month" figure) — the
/// directory snapshots (employees, vehicles, ...) have no natural period and
/// are a roster someone opens on demand, not something to receive on a timer.
/// </summary>
public enum ScheduledReportType
{
    TimeEntries = 1,
    ProjectCosts = 2,
    VehicleCosts = 3,
    MaterialMovements = 4,
    Absences = 5,
    FinanceEntries = 6,
}
