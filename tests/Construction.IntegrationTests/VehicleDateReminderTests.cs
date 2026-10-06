using Construction.Application.Features.Vehicles.Commands.SendVehicleDateReminders;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The office is told when a vehicle's registration runs out, a rental ends, or a vehicle rented out is
/// due back: once 30 days before and once 7 days before, never twice for the same date.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class VehicleDateReminderTests : IntegrationTestBase
{
    public VehicleDateReminderTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<(User Admin, Vehicle Vehicle)> SeedAsync(
        Action<Vehicle> configure)
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var vehicle = await InScope(async scope =>
        {
            var v = await TestData.SeedVehicleAsync(scope);
            configure(v);
            await scope.Db.SaveChangesAsync();
            return v;
        });

        return (admin, vehicle);
    }

    private Task<int> SweepAsync() =>
        InScope(scope => scope.Send(new SendVehicleDateRemindersCommand()));

    /// <summary>One field of the notice's payload (jsonb stores it with its own spacing, so it is parsed, not matched).</summary>
    private static string? Field(Notification notice, string name) =>
        JsonDocument.Parse(notice.DataJson!).RootElement.GetProperty(name).GetString();

    private async Task<List<Notification>> NoticesAboutAsync(Guid userId, Guid vehicleId)
    {
        // DataJson is a jsonb column, which SQL cannot pattern-match; the vehicle is picked out here.
        var all = await InScope(scope => scope.Db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.Type == NotificationType.VehicleDateExpiring)
            .ToListAsync());

        return all.Where(n => n.DataJson!.Contains(vehicleId.ToString())).ToList();
    }

    [Fact]
    public async Task A_registration_ending_within_a_month_is_announced_once()
    {
        var (admin, vehicle) = await SeedAsync(v => v.RegistrationValidUntil = Today.AddDays(20));

        await SweepAsync();
        await SweepAsync();

        var notices = await NoticesAboutAsync(admin.Id, vehicle.Id);
        var notice = Assert.Single(notices);
        Assert.Equal("Registration", Field(notice, "kind"));
        Assert.Equal("20", Field(notice, "daysLeft"));
    }

    [Theory]
    [InlineData("TechnicalInspection")]
    [InlineData("Insurance")]
    [InlineData("Service")]
    public async Task The_other_dates_a_vehicle_carries_are_announced_too(string kind)
    {
        var (admin, vehicle) = await SeedAsync(v =>
        {
            v.TechnicalInspectionValidUntil = kind == "TechnicalInspection" ? Today.AddDays(15) : null;
            v.InsuranceValidUntil = kind == "Insurance" ? Today.AddDays(15) : null;
            v.NextServiceDue = kind == "Service" ? Today.AddDays(15) : null;
        });

        await SweepAsync();

        var notice = Assert.Single(await NoticesAboutAsync(admin.Id, vehicle.Id));
        Assert.Equal(kind, Field(notice, "kind"));
    }

    [Fact]
    public async Task The_final_week_gets_a_second_notice_but_never_a_third()
    {
        var (admin, vehicle) = await SeedAsync(v => v.RegistrationValidUntil = Today.AddDays(20));
        await SweepAsync();

        // Time passes: the same date is now five days off.
        await InScope(async scope =>
        {
            var v = await scope.Db.Vehicles.FirstAsync(x => x.Id == vehicle.Id);
            v.RegistrationValidUntil = Today.AddDays(5);
            await scope.Db.SaveChangesAsync();
        });

        // A changed date is a new date, so it earns its own notices rather than being suppressed by the old.
        await SweepAsync();
        await SweepAsync();

        var notices = await NoticesAboutAsync(admin.Id, vehicle.Id);
        Assert.Equal(2, notices.Count);
    }

    [Fact]
    public async Task A_date_entered_inside_the_last_week_gets_only_the_urgent_notice()
    {
        var (admin, vehicle) = await SeedAsync(v => v.RegistrationValidUntil = Today.AddDays(3));

        await SweepAsync();
        await SweepAsync();

        Assert.Single(await NoticesAboutAsync(admin.Id, vehicle.Id));
    }

    [Fact]
    public async Task A_date_further_than_a_month_off_says_nothing_yet()
    {
        var (admin, vehicle) = await SeedAsync(v => v.RegistrationValidUntil = Today.AddDays(90));

        await SweepAsync();

        Assert.Empty(await NoticesAboutAsync(admin.Id, vehicle.Id));
    }

    [Fact]
    public async Task A_date_that_has_already_passed_is_not_announced()
    {
        var (admin, vehicle) = await SeedAsync(v => v.RegistrationValidUntil = Today.AddDays(-2));

        await SweepAsync();

        Assert.Empty(await NoticesAboutAsync(admin.Id, vehicle.Id));
    }

    [Fact]
    public async Task The_end_of_a_rental_is_announced_for_a_rented_vehicle_but_not_an_owned_one()
    {
        var (admin, rented) = await SeedAsync(v =>
        {
            v.OwnershipType = VehicleOwnershipType.Rented;
            v.RentedUntil = Today.AddDays(10);
        });
        var (_, owned) = await SeedAsync(v =>
        {
            v.OwnershipType = VehicleOwnershipType.Owned;
            v.RentedUntil = Today.AddDays(10);
        });

        await SweepAsync();

        var notice = Assert.Single(await NoticesAboutAsync(admin.Id, rented.Id));
        Assert.Equal("RentedUntil", Field(notice, "kind"));
        Assert.Empty(await NoticesAboutAsync(admin.Id, owned.Id));
    }

    [Fact]
    public async Task A_vehicle_rented_out_is_announced_while_it_is_still_out_and_not_after_it_returned()
    {
        var (admin, vehicle) = await SeedAsync(_ => { });
        var (_, returned) = await SeedAsync(_ => { });

        await InScope(async scope =>
        {
            scope.Db.VehicleRentalsOut.AddRange(
                new VehicleRentalOut
                {
                    VehicleId = vehicle.Id,
                    RenterName = "Gradnja d.o.o.",
                    DailyRate = 50m,
                    StartDate = Today.AddDays(-5),
                    ExpectedEndDate = Today.AddDays(4)
                },
                new VehicleRentalOut
                {
                    VehicleId = returned.Id,
                    RenterName = "Gradnja d.o.o.",
                    DailyRate = 50m,
                    StartDate = Today.AddDays(-5),
                    EndDate = Today.AddDays(-1),
                    ExpectedEndDate = Today.AddDays(4)
                });
            await scope.Db.SaveChangesAsync();
        });

        await SweepAsync();

        var notice = Assert.Single(await NoticesAboutAsync(admin.Id, vehicle.Id));
        Assert.Equal("RentalOutReturn", Field(notice, "kind"));
        Assert.Empty(await NoticesAboutAsync(admin.Id, returned.Id));
    }

    [Fact]
    public async Task Only_the_people_who_run_the_office_are_told()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var (_, vehicle) = await SeedAsync(v => v.RegistrationValidUntil = Today.AddDays(12));

        await SweepAsync();

        Assert.Empty(await NoticesAboutAsync(foreman.Id, vehicle.Id));
    }
}
