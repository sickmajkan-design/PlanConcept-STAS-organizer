using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Planning;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>What happens to equipment when the planner decides: stay, hand over, or back to the store.</summary>
[Collection(DatabaseCollection.Name)]
public class DecideEquipmentTests : IntegrationTestBase
{
    public DecideEquipmentTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>A holder with a vehicle and a tool, both on the holder's site; and a receiver posted to another site today.</summary>
    private async Task<(Employee Holder, Employee Receiver, Project HolderSite, Project ReceiverSite, Vehicle Vehicle, Tool Tool)> SeedAsync()
    {
        var holder = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var receiver = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var holderSite = await InScope(scope => TestData.SeedProjectAsync(scope));
        var receiverSite = await InScope(scope => TestData.SeedProjectAsync(scope));
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var tool = await InScope(scope => TestData.SeedToolAsync(scope));

        await InScopeAs(UserRole.Admin, scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = receiver.Id, ProjectId = receiverSite.Id, From = Today.AddDays(-1), To = Today.AddDays(1) }));

        await InScope(async scope =>
        {
            await scope.Db.Vehicles.Where(v => v.Id == vehicle.Id).ExecuteUpdateAsync(v => v
                .SetProperty(x => x.AssignedEmployeeId, holder.Id).SetProperty(x => x.AssignedProjectId, holderSite.Id)
                .SetProperty(x => x.Status, VehicleStatus.Assigned));
            await scope.Db.Tools.Where(t => t.Id == tool.Id).ExecuteUpdateAsync(t => t
                .SetProperty(x => x.AssignedEmployeeId, holder.Id).SetProperty(x => x.AssignedProjectId, holderSite.Id)
                .SetProperty(x => x.Status, ToolStatus.Assigned));
            return 0;
        });

        return (holder, receiver, holderSite, receiverSite, vehicle, tool);
    }

    private static EquipmentChoiceItem Item(string kind, Guid id, EquipmentChoice choice, Guid? to = null) =>
        new() { Kind = kind, Id = id, Choice = choice, ToEmployeeId = to };

    private Task<(Guid? Employee, Guid? Project)> VehicleNow(Guid id) =>
        InScope(async scope =>
        {
            var v = await scope.Db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == id);
            return (v.AssignedEmployeeId, v.AssignedProjectId);
        });

    private Task<(Guid? Employee, Guid? Project)> ToolNow(Guid id) =>
        InScope(async scope =>
        {
            var t = await scope.Db.Tools.AsNoTracking().SingleAsync(x => x.Id == id);
            return (t.AssignedEmployeeId, t.AssignedProjectId);
        });

    [Fact]
    public async Task Handing_equipment_over_gives_it_to_the_receiver_and_puts_it_on_the_receivers_site()
    {
        var s = await SeedAsync();

        var changed = await InScopeAs(UserRole.Admin, scope => scope.Send(new DecideEquipmentCommand
        {
            Items = [Item("Vehicle", s.Vehicle.Id, EquipmentChoice.HandOver, s.Receiver.Id), Item("Tool", s.Tool.Id, EquipmentChoice.HandOver, s.Receiver.Id)],
        }));

        Assert.Equal(2, changed);
        Assert.Equal((s.Receiver.Id, s.ReceiverSite.Id), await VehicleNow(s.Vehicle.Id));
        Assert.Equal((s.Receiver.Id, s.ReceiverSite.Id), await ToolNow(s.Tool.Id));
    }

    [Fact]
    public async Task Returning_equipment_to_the_store_leaves_it_with_nobody_and_on_no_site()
    {
        var s = await SeedAsync();

        var changed = await InScopeAs(UserRole.Admin, scope => scope.Send(new DecideEquipmentCommand
        {
            Items = [Item("Vehicle", s.Vehicle.Id, EquipmentChoice.ToStore), Item("Tool", s.Tool.Id, EquipmentChoice.ToStore)],
        }));

        Assert.Equal(2, changed);
        Assert.Equal((null, null), await VehicleNow(s.Vehicle.Id));
        Assert.Equal((null, null), await ToolNow(s.Tool.Id));
    }

    [Fact]
    public async Task Choosing_to_leave_equipment_where_it_is_changes_nothing()
    {
        var s = await SeedAsync();

        var changed = await InScopeAs(UserRole.Admin, scope => scope.Send(new DecideEquipmentCommand
        {
            Items = [Item("Tool", s.Tool.Id, EquipmentChoice.Stay)],
        }));

        Assert.Equal(0, changed);
        Assert.Equal((s.Holder.Id, s.HolderSite.Id), await ToolNow(s.Tool.Id));
    }

    [Fact]
    public async Task A_hand_over_without_a_receiver_is_refused_and_so_is_a_worker()
    {
        var s = await SeedAsync();

        await Assert.ThrowsAsync<ValidationException>(() => InScopeAs(UserRole.Admin, scope => scope.Send(new DecideEquipmentCommand
        {
            Items = [Item("Tool", s.Tool.Id, EquipmentChoice.HandOver)],
        })));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScopeAs(UserRole.Worker, scope => scope.Send(new DecideEquipmentCommand
        {
            Items = [Item("Tool", s.Tool.Id, EquipmentChoice.ToStore)],
        })));

        Assert.Equal((s.Holder.Id, s.HolderSite.Id), await ToolNow(s.Tool.Id));
    }
}
