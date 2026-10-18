using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One figure as one user last saw it on a given day, so the next day's board can say how it changed
/// "since yesterday". Per-user display state, not reportable data: the client records what it showed, the
/// server keeps the last value of each day for a few days and hands yesterday's back.
/// </summary>
public class DashboardSnapshot : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>The UTC day the value was last recorded on.</summary>
    public DateOnly Day { get; set; }

    /// <summary>Which figure, including the scope it was shown for, e.g. "kpi.activeEmployees|all".</summary>
    public string Key { get; set; } = null!;

    public decimal Value { get; set; }
}
