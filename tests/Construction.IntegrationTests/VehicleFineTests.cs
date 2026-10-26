using System.Text.Json;
using Construction.Application.Common;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.VehicleFines;
using Construction.Application.Features.Vehicles.Queries.GetVehicles;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Fines a vehicle has collected: entered by the office, who bears them, what the vehicles list says about them, and
/// the reminders before they fall due.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class VehicleFineTests : IntegrationTestBase
{
    public VehicleFineTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly Today = CompanyTime.LocalDate(DateTime.UtcNow);

    private Task<VehicleFineDto> AddAsync(Guid vehicleId, Action<AddFine>? tweak = null, User? by = null) =>
        InScope(async scope =>
        {
            var user = by ?? await TestData.SeedUserAsync(scope, UserRole.Admin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);
            var f = new AddFine();
            tweak?.Invoke(f);

            return await scope.Send(new AddVehicleFineCommand
            {
                VehicleId = vehicleId,
                OccurredOn = Today.AddDays(-3),
                Description = "Prekoračenje brzine",
                Amount = 120m,
                DueDate = f.DueDate ?? Today.AddDays(20),
                DriverEmployeeId = f.Driver,
                BorneBy = f.Bearer,
            });
        });

    private sealed class AddFine
    {
        public DateOnly? DueDate { get; set; }
        public Guid? Driver { get; set; }
        public VehicleFineBearer Bearer { get; set; } = VehicleFineBearer.Firm;
    }

    [Fact]
    public async Task A_fine_borne_by_the_driver_has_to_name_the_driver()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));

        await Assert.ThrowsAsync<ValidationException>(() => AddAsync(vehicle.Id, f => f.Bearer = VehicleFineBearer.Driver));

        var driver = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var fine = await AddAsync(vehicle.Id, f =>
        {
            f.Bearer = VehicleFineBearer.Driver;
            f.Driver = driver.Id;
        });

        Assert.Equal("Driver", fine.BorneBy);
        Assert.Equal(driver.Id, fine.DriverEmployeeId);
    }

    [Fact]
    public async Task The_other_admins_are_told_about_a_new_fine_but_not_the_one_who_entered_it()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var colleague = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var enteredBy = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await AddAsync(vehicle.Id, by: enteredBy);

        var told = await InScope(scope => scope.Db.Notifications.AsNoTracking()
            .Where(n => n.Type == NotificationType.VehicleFineAdded).ToListAsync());

        Assert.Contains(told, n => n.UserId == colleague.Id && n.DataJson!.Contains(vehicle.Id.ToString()));
        Assert.DoesNotContain(told, n => n.UserId == enteredBy.Id);
    }

    [Fact]
    public async Task Open_fines_come_first_and_one_past_its_due_date_is_overdue()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        await AddAsync(vehicle.Id, f => f.DueDate = Today.AddDays(30));
        var late = await AddAsync(vehicle.Id, f => f.DueDate = Today.AddDays(-2));

        var list = await InScope(scope => scope.Send(new GetVehicleFinesQuery(vehicle.Id)));

        Assert.Equal(late.Id, list[0].Id);
        Assert.True(list[0].Overdue);
        Assert.False(list[1].Overdue);
    }

    [Fact]
    public async Task Paying_a_fine_takes_it_off_the_open_count_of_the_vehicle()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var first = await AddAsync(vehicle.Id);
        await AddAsync(vehicle.Id, f => f.DueDate = Today.AddDays(-1));

        async Task<(int Open, decimal Total, int Overdue)> CountsAsync()
        {
            var page = await InScope(scope => scope.Send(new GetVehiclesQuery { PageSize = 100, Search = vehicle.RegistrationNumber }));
            var v = page.Items.Single(x => x.Id == vehicle.Id);

            return (v.OpenFineCount, v.OpenFineTotal, v.OverdueFineCount);
        }

        Assert.Equal((2, 240m, 1), await CountsAsync());

        await InScope(scope => scope.Send(new UpdateVehicleFineCommand
        {
            Id = first.Id,
            OccurredOn = first.OccurredOn,
            Description = first.Description,
            Amount = first.Amount,
            DueDate = first.DueDate,
            Status = VehicleFineStatus.Paid,
        }));

        Assert.Equal((1, 120m, 1), await CountsAsync());
    }

    [Fact]
    public async Task A_vehicle_with_an_open_fine_needs_attention_and_one_without_anything_does_not()
    {
        var withFine = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var plain = await InScope(scope => TestData.SeedVehicleAsync(scope));
        await AddAsync(withFine.Id);

        var page = await InScope(scope => scope.Send(new GetVehiclesQuery { PageSize = 100, NeedsAttention = true }));

        Assert.Contains(page.Items, v => v.Id == withFine.Id);
        Assert.DoesNotContain(page.Items, v => v.Id == plain.Id);
    }

    private Task<List<Notification>> DueNoticesAsync(Guid userId, Guid vehicleId) =>
        InScope(async scope => (await scope.Db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId && n.Type == NotificationType.VehicleFineDue).ToListAsync())
            .Where(n => n.DataJson!.Contains(vehicleId.ToString())).ToList());

    [Fact]
    public async Task A_fine_close_to_its_due_date_is_announced_at_a_week_and_at_two_days_and_never_twice()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        await AddAsync(vehicle.Id, f => f.DueDate = Today.AddDays(6));

        await InScope(scope => scope.Send(new SendVehicleFineRemindersCommand()));
        await InScope(scope => scope.Send(new SendVehicleFineRemindersCommand()));

        Assert.Single(await DueNoticesAsync(admin.Id, vehicle.Id));

        // Two days from the date: the second stage is announced, once.
        await InScope(async scope =>
        {
            var fine = await scope.Db.VehicleFines.SingleAsync(f => f.VehicleId == vehicle.Id);
            fine.DueDate = Today.AddDays(1);
            await scope.Db.SaveChangesAsync();
        });
        await InScope(scope => scope.Send(new SendVehicleFineRemindersCommand()));
        await InScope(scope => scope.Send(new SendVehicleFineRemindersCommand()));

        var notices = await DueNoticesAsync(admin.Id, vehicle.Id);
        Assert.Equal(2, notices.Count);
        Assert.Equal("1", JsonDocument.Parse(notices[^1].DataJson!).RootElement.GetProperty("daysLeft").GetString());
    }

    [Fact]
    public async Task A_paid_or_contested_fine_is_not_reminded_about()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var fine = await AddAsync(vehicle.Id, f => f.DueDate = Today.AddDays(1));

        await InScope(scope => scope.Send(new UpdateVehicleFineCommand
        {
            Id = fine.Id,
            OccurredOn = fine.OccurredOn,
            Description = fine.Description,
            Amount = fine.Amount,
            DueDate = fine.DueDate,
            Status = VehicleFineStatus.Contested,
        }));
        await InScope(scope => scope.Send(new SendVehicleFineRemindersCommand()));

        Assert.Empty(await DueNoticesAsync(admin.Id, vehicle.Id));
    }
}
