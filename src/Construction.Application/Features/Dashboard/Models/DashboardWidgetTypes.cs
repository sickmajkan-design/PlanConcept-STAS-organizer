namespace Construction.Application.Features.Dashboard.Models;

/// <summary>
/// The closed set of widget types a dashboard layout may reference. Kept as
/// plain strings (not a Domain enum) because they only ever travel between
/// this feature's DTOs and the opaque <c>WidgetsJson</c> blob — nothing
/// relational ever queries by type.
/// </summary>
public static class DashboardWidgetTypes
{
    public const string ProjectsRealization = nameof(ProjectsRealization);
    public const string AbsencesBalance = nameof(AbsencesBalance);
    public const string NotificationsBulletin = nameof(NotificationsBulletin);
    public const string DocumentExpiry = nameof(DocumentExpiry);
    public const string FleetStatus = nameof(FleetStatus);
    public const string NeedsAttention = nameof(NeedsAttention);

    public static readonly IReadOnlyList<string> All =
    [
        ProjectsRealization,
        AbsencesBalance,
        NotificationsBulletin,
        DocumentExpiry,
        FleetStatus,
        NeedsAttention,
    ];
}
