using System.Text.Json;
using System.Text.Json.Serialization;
using Construction.API.Authorization;
using Construction.Application.Features.Absences.Queries.GetAbsences;
using Construction.Application.Features.Assignments.Queries.GetAssignmentBoard;
using Construction.Application.Features.Employees.Queries.GetEmployeeById;
using Construction.Application.Features.Employees.Queries.GetEmployees;
using Construction.Application.Features.Materials.Queries.GetMaterials;
using Construction.Application.Features.Projects.Queries.GetProjectById;
using Construction.Application.Features.Projects.Queries.GetProjects;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntries;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntrySummary;
using Construction.Application.Features.Tools.Queries.GetTools;
using Construction.Application.Features.Vehicles.Queries.GetVehicles;
using Construction.Application.Features.WorkItems.Queries.GetWorkItems;
using MediatR;

namespace Construction.API.Assistant;

/// <summary>
/// One tool: an existing query, and the policy its controller already carries.
/// </summary>
public sealed record AssistantTool(
    string Name,
    string Description,
    string InputSchemaJson,
    string RequiredPolicy,
    Type QueryType,
    Func<string, ISender, CancellationToken, Task<object?>> InvokeAsync);

/// <summary>
/// Every tool the assistant has, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// An allow-list, deliberately. Nothing is excluded here by being filtered
/// out — <c>Features/Locations</c>, the cost and pay-rate queries, user and
/// role administration, the audit trail, the privacy erasure path and the
/// spreadsheet exports are simply not written down, so no future refactor can
/// widen the surface without someone typing a new entry into this file and
/// changing <c>AssistantToolRegistryTests</c> along with it.
/// </para>
/// <para>
/// <b>The policy strings are copied from the controllers and must stay copies.</b>
/// They are what puts the role check back for a call that reaches
/// <c>IMediator</c> without passing an action, and there is no compiler to
/// notice if a controller's policy is later tightened and this file is not.
/// <c>AssistantToolRegistryTests</c> pins each one for that reason. Note that
/// <c>AssignmentsController</c> carries its policy on the class rather than the
/// action, which is why <c>list_assignments</c> below is Project&#160;Manager
/// and above rather than the Foreman its method attributes would suggest.
/// </para>
/// </remarks>
public static class AssistantToolRegistry
{
    private static readonly JsonSerializerOptions QueryOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static readonly IReadOnlyList<AssistantTool> All =
    [
        Query<GetProjectsQuery>(
            "list_projects",
            Policies.ForemanAndAbove,
            "List building sites, newest first. Use it to find a site's id before asking about its crew, hours or tasks, and to answer questions about which sites are active.",
            """
            {
              "type": "object",
              "properties": {
                "search": { "type": "string", "description": "Matches the site name or client." },
                "status": { "type": "string", "enum": ["Planned", "Active", "OnHold", "Completed", "Cancelled"] },
                "employeeId": { "type": "string", "description": "Only sites this employee is posted to." },
                "pageSize": { "type": "integer", "description": "Up to 100. Default 20." }
              }
            }
            """),

        Query<GetProjectByIdQuery>(
            "get_project",
            Policies.ForemanAndAbove,
            "One site in full, including the crew currently posted to it and its dates.",
            IdSchema("The site's id, as returned by list_projects.")),

        Query<GetEmployeesQuery>(
            "list_employees",
            Policies.ForemanAndAbove,
            "The staff directory: who works here, their position and their status. Use it to turn a person's name into an id.",
            """
            {
              "type": "object",
              "properties": {
                "search": { "type": "string", "description": "Matches name, employee number or position." },
                "status": { "type": "string", "enum": ["Active", "OnLeave", "Suspended", "Terminated"] },
                "position": { "type": "string" },
                "projectId": { "type": "string", "description": "Only people posted to this site." },
                "pageSize": { "type": "integer", "description": "Up to 100. Default 20." }
              }
            }
            """),

        Query<GetEmployeeByIdQuery>(
            "get_employee",
            Policies.ForemanAndAbove,
            "One person's record, including their current and past site postings.",
            IdSchema("The employee's id, as returned by list_employees.")),

        Query<GetTimeEntriesQuery>(
            "list_time_entries",
            Policies.AllEmployees,
            "Individual shifts: who worked, when, on which site, and whether the entry has been reviewed. For a total across a period prefer time_entry_summary, which is far cheaper than paging through shifts.",
            """
            {
              "type": "object",
              "properties": {
                "employeeId": { "type": "string" },
                "projectId": { "type": "string" },
                "status": { "type": "string", "enum": ["InProgress", "Submitted", "Approved", "Rejected"] },
                "from": { "type": "string", "description": "ISO date-time, inclusive." },
                "to": { "type": "string", "description": "ISO date-time, inclusive." },
                "openOnly": { "type": "boolean", "description": "Only shifts still running." },
                "pageSize": { "type": "integer", "description": "Up to 100. Default 20." }
              }
            }
            """),

        Query<GetTimeEntrySummaryQuery>(
            "time_entry_summary",
            Policies.AllEmployees,
            "Hours totalled per person over a period, computed in the database. Reports approved minutes separately from the total, and how many entries are still awaiting review — say which of the two you are quoting.",
            """
            {
              "type": "object",
              "properties": {
                "from": { "type": "string", "description": "ISO date-time, inclusive. Required." },
                "to": { "type": "string", "description": "ISO date-time, inclusive. Required. At most one year after 'from'." },
                "employeeId": { "type": "string" },
                "projectId": { "type": "string" },
                "approvedOnly": { "type": "boolean", "description": "Count only reviewed and approved entries." }
              },
              "required": ["from", "to"]
            }
            """),

        Query<GetWorkItemsQuery>(
            "list_work_items",
            Policies.AllEmployees,
            "Tasks and reported defects: what is open, who it is assigned to, what is overdue.",
            """
            {
              "type": "object",
              "properties": {
                "search": { "type": "string" },
                "kind": { "type": "string", "enum": ["Task", "Defect"] },
                "status": { "type": "string", "enum": ["Open", "InProgress", "Resolved", "Closed", "Cancelled"] },
                "priority": { "type": "string", "enum": ["Low", "Normal", "High", "Urgent"] },
                "projectId": { "type": "string" },
                "assignedEmployeeId": { "type": "string" },
                "openOnly": { "type": "boolean" },
                "overdueOnly": { "type": "boolean" },
                "pageSize": { "type": "integer", "description": "Up to 100. Default 20." }
              }
            }
            """),

        Query<GetAbsencesQuery>(
            "list_absences",
            Policies.AllEmployees,
            "Leave: requested, approved and refused, with dates and type. Only approved leave means somebody is actually away.",
            """
            {
              "type": "object",
              "properties": {
                "employeeId": { "type": "string" },
                "status": { "type": "string", "enum": ["Requested", "Approved", "Rejected", "Cancelled"] },
                "type": { "type": "string", "enum": ["AnnualLeave", "SickLeave", "UnpaidLeave", "PaidSpecialLeave", "Training", "Other"] },
                "from": { "type": "string", "description": "ISO date (yyyy-MM-dd), inclusive." },
                "to": { "type": "string", "description": "ISO date (yyyy-MM-dd), inclusive." },
                "pageSize": { "type": "integer", "description": "Up to 100. Default 20." }
              }
            }
            """),

        Query<GetAssignmentBoardQuery>(
            "list_assignments",
            Policies.ProjectManagerAndAbove,
            "Today's assignment board: every open site, who is posted to each, and who is currently unassigned. Takes no arguments and always describes today.",
            EmptySchema),

        Query<GetMaterialsQuery>(
            "list_materials",
            Policies.ForemanAndAbove,
            "Warehouse stock: what is held, in what unit, and how much is left.",
            SearchOnlySchema("Matches the material name or code.")),

        Query<GetVehiclesQuery>(
            "list_vehicles",
            Policies.ForemanAndAbove,
            "The fleet: registration, type, status and who currently holds each vehicle.",
            SearchOnlySchema("Matches the registration, make or model.")),

        Query<GetToolsQuery>(
            "list_tools",
            Policies.ForemanAndAbove,
            "Tools and equipment: what exists, its status, and who currently holds it.",
            SearchOnlySchema("Matches the tool name, code or serial number.")),
    ];

    /// <summary>For a query that takes no arguments at all.</summary>
    private const string EmptySchema = """{ "type": "object", "properties": {} }""";

    public static AssistantTool? Find(string name) =>
        All.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.Ordinal));

    private static string IdSchema(string description) =>
        $$"""
        {
          "type": "object",
          "properties": {
            "id": { "type": "string", "description": "{{description}}" }
          },
          "required": ["id"]
        }
        """;

    private static string SearchOnlySchema(string description) =>
        $$"""
        {
          "type": "object",
          "properties": {
            "search": { "type": "string", "description": "{{description}}" },
            "pageSize": { "type": "integer", "description": "Up to 100. Default 20." }
          }
        }
        """;

    /// <summary>
    /// Binds the model's arguments straight onto the query record.
    /// </summary>
    /// <remarks>
    /// The tool's arguments and the query object are the same shape by design,
    /// so there is no hand-written mapping per tool to drift out of step. What
    /// the model sends is then validated by the query's own FluentValidation
    /// rules through the MediatR pipeline — a page size of 5000 or a reversed
    /// date range is refused for the assistant exactly as it is for the panel.
    /// </remarks>
    private static AssistantTool Query<TQuery>(
        string name,
        string policy,
        string description,
        string schemaJson)
        where TQuery : notnull
    {
        return new AssistantTool(
            name,
            description,
            schemaJson,
            policy,
            typeof(TQuery),
            async (argumentsJson, mediator, cancellationToken) =>
            {
                var query = JsonSerializer.Deserialize<TQuery>(argumentsJson, QueryOptions)
                    ?? throw new JsonException(
                        $"The arguments for '{name}' could not be read as a request.");

                return await mediator.Send(query, cancellationToken);
            });
    }
}
