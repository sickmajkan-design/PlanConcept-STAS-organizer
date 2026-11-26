using Construction.Application.Features.Planning;
using Construction.Application.Features.Tools.Commands.CreateTool;
using Construction.Application.Features.Tools.Commands.UpdateTool;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>The planning view carries who holds which vehicle and tool, so the board can draw icons and say what is free.</summary>
[Collection(DatabaseCollection.Name)]
public class PlanningEquipmentTests : IntegrationTestBase
{
    public PlanningEquipmentTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly Day = new(2031, 6, 10);

    private Task<PlanningDto> Plan() =>
        InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningQuery { From = Day, To = Day }));

    [Fact]
    public async Task Vehicles_and_tools_reach_the_planning_view_with_their_holder_site_and_kind()
    {
        var holder = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope, "PE-VEH-1"));
        var kit = await InScope(scope => TestData.SeedToolAsync(scope, "PE kofer"));
        var drill = await InScope(scope => TestData.SeedToolAsync(scope, "PE aku set"));
        var gone = await InScope(scope => TestData.SeedToolAsync(scope, "PE retired"));

        await InScope(async scope =>
        {
            await scope.Db.Vehicles.Where(v => v.Id == vehicle.Id).ExecuteUpdateAsync(v => v
                .SetProperty(x => x.AssignedEmployeeId, holder.Id).SetProperty(x => x.AssignedProjectId, site.Id));
            await scope.Db.Tools.Where(t => t.Id == kit.Id).ExecuteUpdateAsync(t => t
                .SetProperty(x => x.IsKit, true).SetProperty(x => x.AssignedProjectId, site.Id));
            await scope.Db.Tools.Where(t => t.Id == drill.Id).ExecuteUpdateAsync(t => t
                .SetProperty(x => x.Group, ToolGroup.BatterySet));
            await scope.Db.Tools.Where(t => t.Id == gone.Id).ExecuteUpdateAsync(t => t
                .SetProperty(x => x.Status, ToolStatus.Retired));
            return 0;
        });

        var plan = await Plan();

        var v = Assert.Single(plan.Equipment, e => e.Id == vehicle.Id);
        Assert.Equal("Vehicle", v.Group);
        Assert.Equal(holder.Id, v.EmployeeId);
        Assert.Equal(site.Id, v.ProjectId);
        Assert.Contains("PE-VEH-1", v.Label);

        var k = Assert.Single(plan.Equipment, e => e.Id == kit.Id);
        Assert.Equal("Kit", k.Group);
        Assert.Null(k.EmployeeId);
        Assert.Equal(site.Id, k.ProjectId);

        Assert.Equal("BatterySet", Assert.Single(plan.Equipment, e => e.Id == drill.Id).Group);

        // Retired equipment is not planned with.
        Assert.DoesNotContain(plan.Equipment, e => e.Id == gone.Id);
    }

    [Fact]
    public async Task A_tools_group_is_set_on_creation_and_an_older_client_that_omits_it_does_not_wipe_it()
    {
        var created = await InScopeAs(UserRole.Admin, scope => scope.Send(new CreateToolCommand { Name = "PE mjerni", Group = ToolGroup.MeasuringSet }));
        Assert.Equal("MeasuringSet", created.Group);

        // What an installed phone app sends: no group at all.
        var updated = await InScopeAs(UserRole.Admin, scope => scope.Send(new UpdateToolCommand { Id = created.Id, Name = "PE mjerni 2", Status = ToolStatus.Available }));
        Assert.Equal("PE mjerni 2", updated.Name);
        Assert.Equal("MeasuringSet", updated.Group);

        var changed = await InScopeAs(UserRole.Admin, scope => scope.Send(new UpdateToolCommand { Id = created.Id, Name = "PE mjerni 2", Status = ToolStatus.Available, Group = ToolGroup.CableSet }));
        Assert.Equal("CableSet", changed.Group);
    }
}
