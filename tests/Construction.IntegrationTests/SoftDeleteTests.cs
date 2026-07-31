using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Employees.Commands.CreateEmployee;
using Construction.Application.Features.Employees.Commands.DeleteEmployee;
using Construction.Application.Features.Employees.Queries.GetEmployees;
using Construction.Application.Features.Vehicles.Commands.CreateVehicle;
using Construction.Application.Features.Vehicles.Commands.DeleteVehicle;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Deletes are soft, and the unique indexes are filtered on IsDeleted = false
/// so a business identifier frees up again once its record is removed. Both
/// halves of that only hold against the real schema.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SoftDeleteTests : IntegrationTestBase
{
    public SoftDeleteTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Deleting_an_employee_keeps_the_row_but_hides_it_from_queries()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        // Gone as far as the application is concerned...
        var visible = await InScope(scope =>
            scope.Db.Employees.AnyAsync(e => e.Id == employee.Id));
        Assert.False(visible);

        // ...but still on disk, flagged, so it can be recovered.
        var row = await InScope(scope => scope.Db.Employees
            .IgnoreQueryFilters()
            .SingleAsync(e => e.Id == employee.Id));

        Assert.True(row.IsDeleted);
        Assert.NotNull(row.DeletedAt);
    }

    [Fact]
    public async Task A_deleted_employee_disappears_from_the_paged_list()
    {
        var employee = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, firstName: "Deleteme", lastName: "Soon"));

        var before = await InScope(scope =>
            scope.Send(new GetEmployeesQuery { Search = "Deleteme", PageSize = 50 }));
        Assert.Contains(before.Items, e => e.Id == employee.Id);

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        var after = await InScope(scope =>
            scope.Send(new GetEmployeesQuery { Search = "Deleteme", PageSize = 50 }));
        Assert.DoesNotContain(after.Items, e => e.Id == employee.Id);
    }

    [Fact]
    public async Task An_employee_number_can_be_reused_after_the_holder_is_deleted()
    {
        // The unique index is filtered on IsDeleted = false, so payroll can
        // reissue a number without the soft-deleted row blocking it.
        const string number = "EMP-REUSED-01";

        var original = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, employeeNumber: number));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(original.Id)));

        var recreated = await InScope(scope => scope.Send(new CreateEmployeeCommand
        {
            EmployeeNumber = number,
            FirstName = "Petra",
            LastName = "Kovac",
            Position = "Foreman",
            EmploymentDate = new DateOnly(2026, 1, 15),
            Status = EmployeeStatus.Active
        }));

        Assert.NotEqual(original.Id, recreated.Id);
        Assert.Equal(number, recreated.EmployeeNumber);
    }

    [Fact]
    public async Task An_employee_number_still_cannot_be_taken_twice_while_in_use()
    {
        const string number = "EMP-TAKEN-01";

        await InScope(scope => TestData.SeedEmployeeAsync(scope, employeeNumber: number));

        await Assert.ThrowsAsync<ConflictException>(() =>
            InScope(scope => scope.Send(new CreateEmployeeCommand
            {
                EmployeeNumber = number,
                FirstName = "Someone",
                LastName = "Else",
                Position = "Worker",
                EmploymentDate = new DateOnly(2026, 1, 15),
                Status = EmployeeStatus.Active
            })));
    }

    [Fact]
    public async Task A_registration_number_can_be_reused_after_the_vehicle_is_deleted()
    {
        const string registration = "ZG-REUSED-1";

        var original = await InScope(scope =>
            TestData.SeedVehicleAsync(scope, registrationNumber: registration));

        await InScope(scope => scope.Send(new DeleteVehicleCommand(original.Id)));

        var recreated = await InScope(scope => scope.Send(new CreateVehicleCommand
        {
            Brand = "Iveco",
            Model = "Daily",
            RegistrationNumber = registration,
            FuelType = FuelType.Diesel,
            Status = VehicleStatus.Available
        }));

        Assert.NotEqual(original.Id, recreated.Id);
        Assert.Equal(registration, recreated.RegistrationNumber);
    }

    [Fact]
    public async Task Deleting_a_vehicle_releases_its_employee_assignment()
    {
        var (vehicle, employee) = await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var vehicle = await TestData.SeedVehicleAsync(scope);
            vehicle.AssignedEmployeeId = employee.Id;
            await scope.Db.SaveChangesAsync();
            return (vehicle, employee);
        });

        await InScope(scope => scope.Send(new DeleteVehicleCommand(vehicle.Id)));

        var row = await InScope(scope => scope.Db.Vehicles
            .IgnoreQueryFilters()
            .SingleAsync(v => v.Id == vehicle.Id));

        Assert.True(row.IsDeleted);
        Assert.Null(row.AssignedEmployeeId);

        // The employee themselves is untouched.
        var stillThere = await InScope(scope =>
            scope.Db.Employees.AnyAsync(e => e.Id == employee.Id));
        Assert.True(stillThere);
    }

    [Fact]
    public async Task Deleting_something_that_is_already_gone_reports_not_found()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new DeleteEmployeeCommand(employee.Id))));
    }
}
