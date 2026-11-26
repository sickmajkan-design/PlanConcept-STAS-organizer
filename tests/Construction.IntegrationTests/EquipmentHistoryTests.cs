using Construction.Application.Features.Planning;
using Construction.Application.Features.Vehicles.Commands.AssignVehicle;
using Construction.Application.Features.Vehicles.Commands.UnassignVehicle;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>Equipment by date: the history the database keeps, and changes planned for a later day.</summary>
[Collection(DatabaseCollection.Name)]
public class EquipmentHistoryTests : IntegrationTestBase
{
    public EquipmentHistoryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private Task<List<EquipmentHold>> HoldsOf(Guid itemId) =>
        InScope(scope => scope.Db.EquipmentHolds.AsNoTracking().Where(h => h.ItemId == itemId).OrderBy(h => h.From).ToListAsync());

    private Task<Guid?> HolderOn(Guid vehicleId, DateOnly date) =>
        InScopeAs(UserRole.Admin, async scope =>
        {
            var on = await scope.Send(new GetPlanningEquipmentQuery { Date = date });

            return on.Items.SingleOrDefault(i => i.Id == vehicleId)?.EmployeeId;
        });

    [Fact]
    public async Task A_new_vehicle_starts_in_the_store_and_a_change_on_the_same_day_corrects_that_stretch()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var ivo = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var start = Assert.Single(await HoldsOf(vehicle.Id));
        Assert.Null(start.EmployeeId);
        Assert.Equal(Today, start.From);

        await InScopeAs(UserRole.Admin, scope => scope.Send(new AssignVehicleCommand(vehicle.Id, ivo.Id)));

        var holds = await HoldsOf(vehicle.Id);
        var only = Assert.Single(holds);
        Assert.Equal(ivo.Id, only.EmployeeId);
        Assert.Null(only.To);
    }

    [Fact]
    public async Task An_earlier_day_shows_who_held_the_vehicle_then()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var ana = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var ivo = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScopeAs(UserRole.Admin, scope => scope.Send(new AssignVehicleCommand(vehicle.Id, ana.Id)));

        // Make Ana's stretch start five days ago, as if it had been going on since then.
        await InScope(async scope =>
        {
            await scope.Db.EquipmentHolds.Where(h => h.ItemId == vehicle.Id).ExecuteUpdateAsync(h => h.SetProperty(x => x.From, Today.AddDays(-5)));
            return 0;
        });

        await InScopeAs(UserRole.Admin, scope => scope.Send(new UnassignVehicleCommand(vehicle.Id)));
        await InScopeAs(UserRole.Admin, scope => scope.Send(new AssignVehicleCommand(vehicle.Id, ivo.Id)));

        Assert.Equal(ana.Id, await HolderOn(vehicle.Id, Today.AddDays(-2)));
        Assert.Equal(ivo.Id, await HolderOn(vehicle.Id, Today));

        // Before the history began nobody knows, so the item is not claimed to be anywhere.
        Assert.Null(await InScopeAs(UserRole.Admin, async scope =>
            (await scope.Send(new GetPlanningEquipmentQuery { Date = Today.AddDays(-30) })).Items.SingleOrDefault(i => i.Id == vehicle.Id)));
    }

    [Fact]
    public async Task A_change_planned_for_tomorrow_changes_nothing_today_but_shows_tomorrow_and_is_applied_when_the_day_comes()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var ana = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var ivo = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await InScopeAs(UserRole.Admin, scope => scope.Send(new AssignVehicleCommand(vehicle.Id, ana.Id)));

        var tomorrow = Today.AddDays(1);
        await InScopeAs(UserRole.Admin, scope => scope.Send(new DecideEquipmentCommand
        {
            EffectiveFrom = tomorrow,
            Items = [new EquipmentChoiceItem { Kind = "Vehicle", Id = vehicle.Id, Choice = EquipmentChoice.HandOver, ToEmployeeId = ivo.Id }],
        }));

        Assert.Equal(ana.Id, await HolderOn(vehicle.Id, Today));
        Assert.Equal(ivo.Id, await HolderOn(vehicle.Id, tomorrow));
        Assert.Equal(ana.Id, await HolderOn(vehicle.Id, Today.AddDays(0)));

        var planned = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningEquipmentQuery { Date = Today }));
        Assert.Contains(planned.Planned, c => c.ItemId == vehicle.Id && c.EffectiveFrom == tomorrow && c.EmployeeId == ivo.Id);

        // Nothing is due yet; one day later it is.
        Assert.Equal(0, await InScope(scope => PlannedEquipmentApplier.ApplyDueAsync(scope.Db, scope.Get<Construction.Application.Common.Interfaces.INotificationService>(), Today, default)));
        Assert.Equal(1, await InScope(scope => PlannedEquipmentApplier.ApplyDueAsync(scope.Db, scope.Get<Construction.Application.Common.Interfaces.INotificationService>(), tomorrow, default)));

        var now = await InScope(scope => scope.Db.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id));
        Assert.Equal(ivo.Id, now.AssignedEmployeeId);
        Assert.Equal(VehicleStatus.Assigned, now.Status);
        Assert.DoesNotContain((await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningEquipmentQuery { Date = Today }))).Planned, c => c.ItemId == vehicle.Id);
    }

    [Fact]
    public async Task A_planned_change_can_be_taken_back_and_a_worker_may_not_plan_one()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var ana = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var item = new EquipmentChoiceItem { Kind = "Vehicle", Id = vehicle.Id, Choice = EquipmentChoice.HandOver, ToEmployeeId = ana.Id };

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ForbiddenAccessException>(
            () => InScopeAs(UserRole.Worker, scope => scope.Send(new DecideEquipmentCommand { EffectiveFrom = Today.AddDays(2), Items = [item] })));

        await InScopeAs(UserRole.Admin, scope => scope.Send(new DecideEquipmentCommand { EffectiveFrom = Today.AddDays(2), Items = [item] }));
        var id = (await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningEquipmentQuery { Date = Today }))).Planned.Single(c => c.ItemId == vehicle.Id).Id;

        await InScopeAs(UserRole.Admin, scope => scope.Send(new CancelPlannedEquipmentChangeCommand(id)));

        Assert.DoesNotContain((await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlanningEquipmentQuery { Date = Today }))).Planned, c => c.ItemId == vehicle.Id);
        Assert.Null(await HolderOn(vehicle.Id, Today.AddDays(3)));
    }

    private Task<int> ApplyOn(DateOnly day) =>
        InScope(scope => PlannedEquipmentApplier.ApplyDueAsync(scope.Db, scope.Get<Construction.Application.Common.Interfaces.INotificationService>(), day, default));

    private Task Plan(Guid vehicleId, Guid toEmployee, DateOnly day) =>
        InScopeAs(UserRole.Admin, scope => scope.Send(new DecideEquipmentCommand
        {
            EffectiveFrom = day,
            Items = [new EquipmentChoiceItem { Kind = "Vehicle", Id = vehicleId, Choice = EquipmentChoice.HandOver, ToEmployeeId = toEmployee }],
        }));

    [Fact]
    public async Task A_plan_made_stale_by_a_change_by_hand_is_dropped_instead_of_overriding_it()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var ana = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var ivo = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await Task.Delay(20);
        await Plan(vehicle.Id, ivo.Id, Today.AddDays(1));
        await Task.Delay(20);
        await InScopeAs(UserRole.Admin, scope => scope.Send(new AssignVehicleCommand(vehicle.Id, ana.Id)));

        Assert.Equal(0, await ApplyOn(Today.AddDays(1)));

        var now = await InScope(scope => scope.Db.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id));
        Assert.Equal(ana.Id, now.AssignedEmployeeId);
        Assert.False(await InScope(scope => scope.Db.PlannedEquipmentChanges.AnyAsync(c => c.ItemId == vehicle.Id)));
    }

    [Fact]
    public async Task A_vehicle_that_went_into_service_is_not_handed_over_when_the_plan_comes_due()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var ivo = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await Plan(vehicle.Id, ivo.Id, Today.AddDays(1));
        await InScope(async scope =>
        {
            await scope.Db.Vehicles.Where(v => v.Id == vehicle.Id).ExecuteUpdateAsync(v => v.SetProperty(x => x.Status, VehicleStatus.InService));
            return 0;
        });

        Assert.Equal(0, await ApplyOn(Today.AddDays(1)));

        var now = await InScope(scope => scope.Db.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id));
        Assert.Null(now.AssignedEmployeeId);
        Assert.Equal(VehicleStatus.InService, now.Status);
    }
}
