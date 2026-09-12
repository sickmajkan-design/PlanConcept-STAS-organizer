using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One user's configurable home-dashboard layout — which widgets they've
/// added, and where. A single JSON blob rather than one row per widget: this
/// is per-user UI state nobody else ever queries, not shared/reportable data,
/// so a relational join hierarchy (like the <see cref="Ledger"/> family)
/// would be unwarranted complexity here.
/// </summary>
public class DashboardLayout : BaseEntity, IAuditable
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>
    /// Serialized <c>DashboardWidgetConfig[]</c> — each entry an
    /// <c>{ id, type, column, order }</c> widget instance. Stored as jsonb;
    /// shaped and validated at the application layer, not the database.
    /// </summary>
    public string WidgetsJson { get; set; } = "[]";
}
