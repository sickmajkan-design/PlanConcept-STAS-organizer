namespace Construction.Application.Features.Dashboard.Models;

/// <summary>
/// The closed set of widget types a dashboard layout may reference. Kept as
/// plain strings (not a Domain enum) because they only ever travel between
/// this feature's DTOs and the opaque <c>WidgetsJson</c> blob — nothing
/// relational ever queries by type.
/// </summary>
public static class DashboardWidgetTypes
{
    public const string CompanyKpi = nameof(CompanyKpi);
    public const string ProjectsRealization = nameof(ProjectsRealization);
    public const string AbsencesBalance = nameof(AbsencesBalance);
    public const string NotificationsBulletin = nameof(NotificationsBulletin);
    public const string DocumentExpiry = nameof(DocumentExpiry);
    public const string FleetStatus = nameof(FleetStatus);
    public const string NeedsAttention = nameof(NeedsAttention);
    public const string LiveMap = nameof(LiveMap);
    public const string TodayAttendance = nameof(TodayAttendance);
    public const string CostTrend = nameof(CostTrend);
    public const string FinanceOverview = nameof(FinanceOverview);
    public const string IncomeVsExpense = nameof(IncomeVsExpense);
    public const string TopProjectsByExpense = nameof(TopProjectsByExpense);
    public const string ProfitByProject = nameof(ProfitByProject);
    public const string CostBreakdown = nameof(CostBreakdown);
    public const string ProjectFocus = nameof(ProjectFocus);
    public const string ProjectsOverBudget = nameof(ProjectsOverBudget);
    public const string SpendingTrend = nameof(SpendingTrend);
    public const string FinanceStatistics = nameof(FinanceStatistics);
    public const string AbsenceRequests = nameof(AbsenceRequests);
    public const string ActiveProjects = nameof(ActiveProjects);
    public const string ArticleOrders = nameof(ArticleOrders);
    public const string BranchOverview = nameof(BranchOverview);
    public const string WeeklyReports = nameof(WeeklyReports);
    public const string Finance = nameof(Finance);

    public static readonly IReadOnlyList<string> All =
    [
        CompanyKpi,
        ProjectsRealization,
        AbsencesBalance,
        NotificationsBulletin,
        DocumentExpiry,
        FleetStatus,
        NeedsAttention,
        LiveMap,
        TodayAttendance,
        Finance,
        IncomeVsExpense,
        TopProjectsByExpense,
        ProfitByProject,
        ProjectFocus,
        ProjectsOverBudget,
        AbsenceRequests,
        ActiveProjects,
        ArticleOrders,
        BranchOverview,
        WeeklyReports,
    ];

    /// <summary>
    /// The five finance cards that were folded into <see cref="Finance"/>. They are still accepted when a layout
    /// is saved, so a browser that has not reloaded yet does not get an error, and are replaced by the one card
    /// whenever a layout is read.
    /// </summary>
    public static readonly IReadOnlyList<string> LegacyFinance =
    [
        CostTrend,
        FinanceOverview,
        CostBreakdown,
        SpendingTrend,
        FinanceStatistics,
    ];

    /// <summary>
    /// What a first visit gets: what is looked at every day, in the order it is read. Everything else is one
    /// press away in "Add widget". A longer board was nearly 9,000 px tall on a phone.
    /// </summary>
    public static readonly IReadOnlyList<string> Default =
    [
        NeedsAttention,
        TodayAttendance,
        CompanyKpi,
        ArticleOrders,
        Finance,
        AbsenceRequests,
    ];
}
