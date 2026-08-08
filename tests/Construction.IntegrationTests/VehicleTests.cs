using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Vehicles.Commands.AssignVehicle;
using Construction.Application.Features.Vehicles.Commands.CreateVehicle;
using Construction.Application.Features.Vehicles.Commands.UnassignVehicle;
using Construction.Application.Features.Vehicles.Commands.UpdateVehicle;
using Construction.Application.Features.Vehicles.Queries.GetVehicleById;
using Construction.Application.Features.Vehicles.Queries.GetVehicles;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Who has which van, and the rules about when that can change.
/// </summary>
/// <remarks>
/// Assign and unassign had no tests at all. They are the two commands in this
/// module that mean something to a person rather than to a table: assigning
/// tells somebody a vehicle is theirs, and the status transitions decide
/// whether a van in the workshop can be handed out.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class VehicleTests : IntegrationTestBase
{
    public VehicleTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static UpdateVehicleCommand Edit(
        Guid id,
        string registration,
        string? vin = null,
        string brand = "Ford",
        VehicleStatus status = VehicleStatus.Available) => new()
        {
            Id = id,
            Brand = brand,
            Model = "Transit",
            RegistrationNumber = registration,
            Vin = vin,
            FuelType = FuelType.Diesel,
            Status = status
        };

    private Task<Vehicle> SeedWithStatusAsync(VehicleStatus status) =>
        InScope(async scope =>
        {
            var vehicle = await TestData.SeedVehicleAsync(scope);
            vehicle.Status = status;
            await scope.Db.SaveChangesAsync();
            return vehicle;
        });

    private Task<int> NotificationCountAsync(Guid userId, NotificationType type) =>
        InScope(scope => scope.Db.Notifications
            .CountAsync(n => n.UserId == userId && n.Type == type));

    // ---- assigning -------------------------------------------------------

    [Fact]
    public async Task Assigning_a_vehicle_records_the_driver_and_marks_it_taken()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var employee = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, firstName: "Marko", lastName: "Juric"));

        var result = await InScope(scope =>
            scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        Assert.Equal(employee.Id, result.AssignedEmployeeId);
        Assert.Equal("Marko Juric", result.AssignedEmployeeName);
        Assert.Equal(employee.EmployeeNumber, result.AssignedEmployeeNumber);

        // The status moves with the assignment rather than being set
        // separately, so the two can never disagree.
        Assert.Equal(nameof(VehicleStatus.Assigned), result.Status);
    }

    [Fact]
    public async Task The_driver_is_told_the_vehicle_is_theirs()
    {
        // The point of the command from the driver's side. Without this they
        // find out by being handed a key, which is exactly the coordination
        // the system is meant to remove.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        var (employee, user) = await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var user = await TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id);
            return (employee, user);
        });

        await InScope(scope => scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        Assert.Equal(1, await NotificationCountAsync(user.Id, NotificationType.VehicleAssigned));
    }

    [Fact]
    public async Task A_driver_with_no_account_is_assigned_the_vehicle_anyway()
    {
        // Plenty of employees never sign in. Assignment is a fact about the
        // fleet, not about the app, so it must not depend on there being
        // somebody to notify.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var result = await InScope(scope =>
            scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        Assert.Equal(employee.Id, result.AssignedEmployeeId);
    }

    [Fact]
    public async Task A_deactivated_account_is_not_notified()
    {
        // A disabled account cannot read the notification, and queuing one
        // keeps a push token alive for somebody who has left.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        var (employee, user) = await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var user = await TestData.SeedUserAsync(
                scope, UserRole.Worker, employee.Id, isActive: false);
            return (employee, user);
        });

        await InScope(scope => scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        Assert.Equal(0, await NotificationCountAsync(user.Id, NotificationType.VehicleAssigned));
    }

    [Theory]
    [InlineData(VehicleStatus.InService)]
    [InlineData(VehicleStatus.OutOfService)]
    public async Task A_vehicle_in_the_workshop_cannot_be_handed_to_anybody(VehicleStatus status)
    {
        // The rule that makes the status worth having. Assigning a van that is
        // off the road sends somebody to a depot to collect it.
        var vehicle = await SeedWithStatusAsync(status);
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var error = await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
            scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id))));

        Assert.Contains(status.ToString(), error.Message);
    }

    [Fact]
    public async Task Assigning_the_same_vehicle_to_the_same_person_twice_is_refused()
    {
        // A double-click on the panel would otherwise send a second "this is
        // yours" notification for something that has not changed.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
            scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id))));
    }

    [Fact]
    public async Task A_vehicle_can_be_handed_from_one_driver_to_another_in_one_step()
    {
        // Reassignment is the common case — a van changes crew between jobs —
        // and requiring an unassign first would leave a window where the
        // vehicle looks free.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var first = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var second = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, firstName: "Ana", lastName: "Peric"));

        await InScope(scope => scope.Send(new AssignVehicleCommand(vehicle.Id, first.Id)));

        var result = await InScope(scope =>
            scope.Send(new AssignVehicleCommand(vehicle.Id, second.Id)));

        Assert.Equal(second.Id, result.AssignedEmployeeId);
        Assert.Equal("Ana Peric", result.AssignedEmployeeName);
    }

    [Fact]
    public async Task Assigning_a_vehicle_that_is_not_there_reports_not_found()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
            scope.Send(new AssignVehicleCommand(Guid.NewGuid(), employee.Id))));
    }

    [Fact]
    public async Task Assigning_a_vehicle_to_somebody_who_is_not_there_reports_not_found()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
            scope.Send(new AssignVehicleCommand(vehicle.Id, Guid.NewGuid()))));
    }

    // ---- unassigning -----------------------------------------------------

    [Fact]
    public async Task Handing_a_vehicle_back_returns_it_to_the_pool()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        var result = await InScope(scope => scope.Send(new UnassignVehicleCommand(vehicle.Id)));

        Assert.Null(result.AssignedEmployeeId);
        Assert.Null(result.AssignedEmployeeName);
        Assert.Equal(nameof(VehicleStatus.Available), result.Status);
    }

    [Fact]
    public async Task Handing_back_a_vehicle_that_nobody_has_is_refused()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        await Assert.ThrowsAsync<ConflictException>(() =>
            InScope(scope => scope.Send(new UnassignVehicleCommand(vehicle.Id))));
    }

    [Fact]
    public async Task Handing_a_vehicle_back_does_not_pretend_a_broken_one_is_available()
    {
        // Only the Assigned status is cleared. A van that went to the workshop
        // while assigned is still in the workshop afterwards, and marking it
        // Available would put it back in the pool with a fault on it.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        await InScope(async scope =>
        {
            var tracked = await scope.Db.Vehicles.SingleAsync(v => v.Id == vehicle.Id);
            tracked.Status = VehicleStatus.InService;
            await scope.Db.SaveChangesAsync();
        });

        var result = await InScope(scope => scope.Send(new UnassignVehicleCommand(vehicle.Id)));

        Assert.Null(result.AssignedEmployeeId);
        Assert.Equal(nameof(VehicleStatus.InService), result.Status);
    }

    [Fact]
    public async Task Handing_back_a_vehicle_that_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new UnassignVehicleCommand(Guid.NewGuid()))));
    }

    // ---- editing ---------------------------------------------------------

    [Fact]
    public async Task A_registration_number_is_stored_in_capitals()
    {
        // Plates are matched exactly, and a lower-case entry would be a second
        // vehicle as far as the unique index is concerned.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        var updated = await InScope(scope =>
            scope.Send(Edit(vehicle.Id, "  zg-1234-ab  ", vin: " wf0xxtt0xx000001 ")));

        Assert.Equal("ZG-1234-AB", updated.RegistrationNumber);
        Assert.Equal("WF0XXTT0XX000001", updated.Vin);
    }

    [Fact]
    public async Task An_edit_cannot_take_a_plate_that_another_vehicle_carries()
    {
        var taken = await InScope(scope =>
            TestData.SeedVehicleAsync(scope, registrationNumber: "ZG-TAKEN-99"));
        var other = await InScope(scope => TestData.SeedVehicleAsync(scope));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            InScope(scope => scope.Send(Edit(other.Id, taken.RegistrationNumber))));

        Assert.Contains("ZG-TAKEN-99", error.Message);
    }

    [Fact]
    public async Task An_edit_cannot_take_a_vin_that_another_vehicle_carries()
    {
        const string vin = "WF0XXTT0XXDUPE01";

        var first = await InScope(scope => TestData.SeedVehicleAsync(scope));
        await InScope(scope => scope.Send(Edit(first.Id, first.RegistrationNumber, vin)));

        var second = await InScope(scope => TestData.SeedVehicleAsync(scope));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            InScope(scope => scope.Send(Edit(second.Id, second.RegistrationNumber, vin))));

        Assert.Contains(vin, error.Message);
    }

    [Fact]
    public async Task Two_vehicles_may_both_have_no_vin()
    {
        // The uniqueness check skips a null VIN. Treating "unknown" as a value
        // would let the first vehicle without one block every other.
        var first = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var second = await InScope(scope => TestData.SeedVehicleAsync(scope));

        await InScope(scope => scope.Send(Edit(first.Id, first.RegistrationNumber)));
        var updated = await InScope(scope => scope.Send(Edit(second.Id, second.RegistrationNumber)));

        Assert.Null(updated.Vin);
    }

    [Fact]
    public async Task An_edit_keeping_the_vehicle_s_own_plate_is_not_a_conflict()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        var updated = await InScope(scope =>
            scope.Send(Edit(vehicle.Id, vehicle.RegistrationNumber, brand: "Renault")));

        Assert.Equal("Renault", updated.Brand);
    }

    [Fact]
    public async Task The_status_of_an_assigned_vehicle_cannot_be_changed_behind_the_driver_s_back()
    {
        // Sending a van to the workshop while somebody is driving it would
        // leave the fleet list and the driver disagreeing, with no notification
        // either way. Unassigning first makes that a deliberate act.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new AssignVehicleCommand(vehicle.Id, employee.Id)));

        var error = await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
            scope.Send(Edit(vehicle.Id, vehicle.RegistrationNumber,
                status: VehicleStatus.InService))));

        Assert.Contains("unassign", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Editing_a_vehicle_that_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(Edit(Guid.NewGuid(), "ZG-GHOST-01"))));
    }

    // ---- reading ---------------------------------------------------------

    [Fact]
    public async Task A_created_vehicle_reads_back_as_it_was_written()
    {
        var created = await InScope(scope => scope.Send(new CreateVehicleCommand
        {
            Brand = "Iveco",
            Model = "Daily",
            RegistrationNumber = "zg-9999-zz",
            Vin = "zfa25000002abc123",
            FuelType = FuelType.Diesel,
            Status = VehicleStatus.Available
        }));

        var detail = await InScope(scope => scope.Send(new GetVehicleByIdQuery(created.Id)));

        Assert.Equal("Iveco", detail.Brand);
        Assert.Equal("Daily", detail.Model);
        Assert.Equal("ZG-9999-ZZ", detail.RegistrationNumber);
        Assert.Equal("ZFA25000002ABC123", detail.Vin);
        Assert.Equal(nameof(FuelType.Diesel), detail.FuelType);
    }

    [Fact]
    public async Task Reading_a_vehicle_that_is_not_there_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetVehicleByIdQuery(Guid.NewGuid()))));
    }

    // ---- listing ---------------------------------------------------------

    [Fact]
    public async Task Search_covers_the_make_the_plate_and_the_vin()
    {
        var vehicle = await InScope(scope => scope.Send(new CreateVehicleCommand
        {
            Brand = "Uniquebrand",
            Model = "Uniquemodel",
            RegistrationNumber = "ZG-FINDME-1",
            Vin = "VINFINDME0000001",
            FuelType = FuelType.Petrol,
            Status = VehicleStatus.Available
        }));

        foreach (var term in new[] { "Uniquebrand Uniquemodel", "FINDME", "VINFINDME" })
        {
            var page = await InScope(scope =>
                scope.Send(new GetVehiclesQuery { Search = term, PageSize = 100 }));

            Assert.Contains(page.Items, v => v.Id == vehicle.Id);
        }
    }

    [Fact]
    public async Task The_pool_can_be_listed_separately_from_what_is_out()
    {
        // The unassigned filter is scoped by a plate prefix only this test
        // uses. Asking for every unassigned vehicle would return most of what
        // the suite has ever seeded, and a page holds a hundred.
        var prefix = $"ZG-POOL{Guid.NewGuid().ToString("N")[..4]}";

        var free = await InScope(scope =>
            TestData.SeedVehicleAsync(scope, registrationNumber: $"{prefix}-A"));
        var taken = await InScope(scope =>
            TestData.SeedVehicleAsync(scope, registrationNumber: $"{prefix}-B"));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new AssignVehicleCommand(taken.Id, employee.Id)));

        var pool = await InScope(scope => scope.Send(new GetVehiclesQuery
        {
            Search = prefix,
            Unassigned = true,
            PageSize = 100
        }));

        Assert.Contains(pool.Items, v => v.Id == free.Id);
        Assert.DoesNotContain(pool.Items, v => v.Id == taken.Id);

        var theirs = await InScope(scope => scope.Send(new GetVehiclesQuery
        {
            AssignedEmployeeId = employee.Id,
            PageSize = 100
        }));

        Assert.Contains(theirs.Items, v => v.Id == taken.Id);
        Assert.DoesNotContain(theirs.Items, v => v.Id == free.Id);
    }

    [Fact]
    public async Task The_list_can_be_narrowed_to_a_fuel_type()
    {
        var prefix = $"ZG-FUEL{Guid.NewGuid().ToString("N")[..4]}";

        var diesel = await InScope(scope =>
            TestData.SeedVehicleAsync(scope, registrationNumber: $"{prefix}-D"));

        var electric = await InScope(scope => scope.Send(new CreateVehicleCommand
        {
            Brand = "Nissan",
            Model = "e-NV200",
            RegistrationNumber = $"{prefix}-E",
            FuelType = FuelType.Electric,
            Status = VehicleStatus.Available
        }));

        var page = await InScope(scope => scope.Send(new GetVehiclesQuery
        {
            Search = prefix,
            FuelType = FuelType.Electric,
            PageSize = 100
        }));

        Assert.Contains(page.Items, v => v.Id == electric.Id);
        Assert.DoesNotContain(page.Items, v => v.Id == diesel.Id);
    }
}
