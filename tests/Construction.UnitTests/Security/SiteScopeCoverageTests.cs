using System.Reflection;
using Construction.Application.Common.Behaviours;
using MediatR;

namespace Construction.UnitTests.Security;

/// <summary>
/// A command that names a project is about a site, so a Foreman or Project Manager must be held to their own sites. The rule
/// has been missed on a new command three times; this fails the build when a command that carries a ProjectId is neither
/// <see cref="ISiteScoped"/> nor on the reviewed list below.
/// </summary>
public class SiteScopeCoverageTests
{
    // Reviewed and deliberately not site-scoped. Add a name only with the reason next to it.
    private static readonly Dictionary<string, string> Reviewed = new()
    {
        ["CreateBookEntryCommand"] = "book of money: BookAccess grant checked on each call",
        ["UpdateBookEntryCommand"] = "book of money: BookAccess grant",
        ["UpdateSiteBillingCommand"] = "book of money: BookAccess grant",
        ["RecordFinanceEntryCommand"] = "[FinanceAccess] on the action",
        ["UpdateFinanceEntryCommand"] = "[FinanceAccess] on the action",
        ["RecordGeneralExpenseCommand"] = "[FinanceAccess] on the action",
        ["UpdateGeneralExpenseCommand"] = "[FinanceAccess] on the action",
        ["SetProjectBudgetCommand"] = "finance grant checked in the handler",
        ["RecordProjectRevenueCommand"] = "[FinanceAccess] on the action",
        ["CreateRecurringExpenseCommand"] = "finance grant checked in the handler",
        ["UpdateRecurringExpenseCommand"] = "finance grant checked in the handler",
        ["AddLedgerSectionCommand"] = "SuperAdminOnly",
        ["UpdateLedgerSectionCommand"] = "SuperAdminOnly",
        ["PromoteLedgerRowToGeneralExpenseCommand"] = "SuperAdminOnly",
        ["GetOrCreateSignedTimesheetCommand"] = "SuperAdminOnly",
        ["CreateInvoiceCommand"] = "AdminAndAbove",
        ["SendAnnouncementCommand"] = "AdminAndAbove",
        ["CreateProjectCommand"] = "a new project has no site to protect yet",
        ["CreateRefundCommand"] = "a person files their own refund",
        ["ClockInCommand"] = "a person clocks in themselves, on the sites offered to them",
        ["CreateWeeklySiteReportCommand"] = "the handler refuses a site the author is not posted to",
        ["SetEmployeeScheduleCommand"] = "planning is company-wide for whoever holds the planner grant; decision pending, see open questions",
        ["SetProjectStaffingNeedsCommand"] = "planning is company-wide for whoever holds the planner grant; decision pending, see open questions",
        ["AssignCrewCommand"] = "moves each member through SetEmployeeScheduleCommand, so the same planning rule; crews are kept only by planners not limited to a unit",
    };

    private static IEnumerable<Type> CommandsNamingAProject() =>
        typeof(ISiteScoped).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true }
                && t.Name.EndsWith("Command", StringComparison.Ordinal)
                && t.GetInterfaces().Any(i => i == typeof(IBaseRequest)))
            .Where(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p => p.Name.EndsWith("ProjectId", StringComparison.Ordinal)
                    && (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?))));

    [Fact]
    public void Every_command_that_names_a_project_is_site_scoped_or_reviewed()
    {
        var open = CommandsNamingAProject()
            .Where(t => !typeof(ISiteScoped).IsAssignableFrom(t) && !Reviewed.ContainsKey(t.Name))
            .Select(t => t.FullName!.Replace("Construction.Application.Features.", ""))
            .OrderBy(n => n)
            .ToList();

        Assert.True(open.Count == 0, "Not site-scoped and not reviewed:\n" + string.Join("\n", open));
    }
}
