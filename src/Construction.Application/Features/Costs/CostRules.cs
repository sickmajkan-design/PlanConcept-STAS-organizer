using Construction.Domain.Enums;

namespace Construction.Application.Features.Costs;

/// <summary>
/// Who may see and change money.
/// </summary>
/// <remarks>
/// Costing is the one module where the role hierarchy the rest of the system
/// uses does not apply unchanged. A foreman runs a site and needs the
/// schedule, the hours and the stock; what a colleague earns per hour is a
/// different matter, and one that a construction firm will not want on the
/// phone of every supervisor.
///
/// So the split here is deliberately tighter than <c>ForemanAndAbove</c>:
/// recording what was spent is site work, and reading what it all cost is
/// office work.
/// </remarks>
public static class CostRules
{
    /// <summary>
    /// How far back spending may be recorded.
    /// </summary>
    /// <remarks>
    /// Longer than the timesheets allow, because an invoice for June's fuel
    /// genuinely turns up in August and has to go against June. Still bounded,
    /// so a mistyped year cannot quietly reopen a closed set of books.
    /// </remarks>
    public const int MaxBackdatingDays = 400;

    /// <summary>
    /// Highest hourly rate the system will accept.
    /// </summary>
    /// <remarks>
    /// A guard against a slipped decimal point, not a policy on pay. The
    /// currency is minor units of a single currency — dinars, for the market
    /// this is built for — so the ceiling is deliberately high; the mistake
    /// worth catching is an extra zero or three, which quietly multiplies
    /// every project total that touches this person.
    /// </remarks>
    public const decimal MaxHourlyRate = 1_000_000m;

    /// <summary>
    /// Who may see labour rates and any figure derived from them.
    /// </summary>
    /// <remarks>
    /// A rate is effectively somebody's pay. Project managers price jobs and
    /// need it; foremen do not, and giving it to them puts every colleague's
    /// earnings one tap away on a site phone.
    /// </remarks>
    public static bool CanSeeLabourCost(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin or UserRole.ProjectManager;

    /// <summary>Who may set a rate. Narrower than reading one.</summary>
    public static bool CanSetLabourRate(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin;

    /// <summary>
    /// Who may record what was spent — a delivery, an issue to site, a tank
    /// of diesel.
    /// </summary>
    /// <remarks>
    /// This is the wide one on purpose. A foreman signing for a delivery is
    /// the person who knows it arrived, and making them phone the office
    /// means it gets written on paper and typed in a week later, or not at
    /// all. The figures are only worth reading if they are recorded.
    /// </remarks>
    public static bool CanRecordSpending(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman;

    /// <summary>
    /// Who may see a total that mixes materials and vehicles but not labour.
    /// </summary>
    /// <remarks>
    /// A foreman may see what their site consumed — that is stock control,
    /// and they ordered most of it. The labour half of the same report is
    /// withheld rather than the whole report refused, so the screen is still
    /// useful to the person running the site.
    /// </remarks>
    public static bool CanSeeSpending(UserRole? role) => CanRecordSpending(role);

    /// <summary>Who may correct or remove a recorded amount.</summary>
    /// <remarks>
    /// Narrower than recording one. Entering a wrong figure is a mistake
    /// anyone makes; quietly removing one after the fact is how a total stops
    /// matching the paperwork behind it.
    /// </remarks>
    public static bool CanDeleteSpending(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin or UserRole.ProjectManager;
}
