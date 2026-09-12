using System.Text.Json;
using Construction.API.Assistant;
using Construction.API.Authorization;

namespace Construction.UnitTests.Assistant;

/// <summary>
/// The registry is the assistant's whole reach, so this file freezes it.
/// </summary>
/// <remarks>
/// Two things are pinned, and both are pinned because nothing else would
/// notice them changing. The first is the set of tools: adding one has to be a
/// deliberate act that fails this test, because the moment it fails is the
/// moment somebody asks whether the new tool belongs inside the ceiling the
/// owner set. The second is each tool's policy, which is a hand-copy of the
/// attribute on its controller — there is no compiler check that the copy is
/// still true, and a controller tightened later would otherwise leave the
/// assistant running the old, wider rule.
/// </remarks>
public class AssistantToolRegistryTests
{
    /// <summary>
    /// Every tool, with the policy its controller carries. Deliberately
    /// written out rather than derived: a test that computed this from the
    /// registry would agree with any change, including a wrong one.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Expected =
        new Dictionary<string, string>
        {
            ["list_projects"] = Policies.ForemanAndAbove,
            ["get_project"] = Policies.ForemanAndAbove,
            ["list_employees"] = Policies.ForemanAndAbove,
            ["get_employee"] = Policies.ForemanAndAbove,
            ["list_time_entries"] = Policies.AllEmployees,
            ["time_entry_summary"] = Policies.AllEmployees,
            ["list_work_items"] = Policies.AllEmployees,
            ["list_absences"] = Policies.AllEmployees,

            // Project Manager and above, because AssignmentsController carries
            // its [Authorize] on the class rather than the action. A registry
            // that only read method attributes would have written Foreman here.
            ["list_assignments"] = Policies.ProjectManagerAndAbove,

            ["list_materials"] = Policies.ForemanAndAbove,
            ["list_vehicles"] = Policies.ForemanAndAbove,
            ["list_tools"] = Policies.ForemanAndAbove,
        };

    [Fact]
    public void The_registry_holds_exactly_these_tools()
    {
        Assert.Equal(
            Expected.Keys.OrderBy(name => name, StringComparer.Ordinal),
            AssistantToolRegistry.All.Select(t => t.Name).OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void Each_tool_requires_the_policy_its_controller_requires()
    {
        foreach (var tool in AssistantToolRegistry.All)
        {
            Assert.Equal(Expected[tool.Name], tool.RequiredPolicy);
        }
    }

    [Fact]
    public void No_tool_reads_location_pay_accounts_or_the_audit_trail()
    {
        // The ceiling stated as a rule rather than as a list, so a tool added
        // from one of these areas fails here even if somebody also updated
        // Expected above without thinking about why.
        string[] forbidden =
        [
            "Features.Locations",
            "Features.Costs",
            "Features.Users",
            "Features.Authentication",
            "Features.Audit",
            "Features.Privacy",
            "Features.Exports",
        ];

        foreach (var tool in AssistantToolRegistry.All)
        {
            var target = tool.QueryType.FullName ?? string.Empty;

            foreach (var area in forbidden)
            {
                Assert.DoesNotContain(area, target, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Every_tool_declares_a_usable_object_schema()
    {
        foreach (var tool in AssistantToolRegistry.All)
        {
            var schema = JsonDocument.Parse(tool.InputSchemaJson).RootElement;

            Assert.Equal("object", schema.GetProperty("type").GetString());
            Assert.True(
                schema.TryGetProperty("properties", out _),
                $"'{tool.Name}' has no properties object, which the API rejects.");
        }
    }

    [Fact]
    public void Every_tool_describes_itself_well_enough_to_be_chosen()
    {
        foreach (var tool in AssistantToolRegistry.All)
        {
            // The description is the only thing the model has to pick between
            // twelve tools, so an empty or one-word one is a bug that shows up
            // as bad answers rather than as an error.
            Assert.False(string.IsNullOrWhiteSpace(tool.Description));
            Assert.True(
                tool.Description.Length >= 40,
                $"'{tool.Name}' does not say enough about when to use it.");
        }
    }

    [Fact]
    public void Tool_names_are_unique()
    {
        // Find() takes the first match, so a duplicate would silently shadow.
        Assert.Equal(
            AssistantToolRegistry.All.Count,
            AssistantToolRegistry.All.Select(t => t.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void An_unknown_name_finds_nothing()
    {
        Assert.Null(AssistantToolRegistry.Find("delete_everything"));
        Assert.Null(AssistantToolRegistry.Find("LIST_PROJECTS"));
    }
}
