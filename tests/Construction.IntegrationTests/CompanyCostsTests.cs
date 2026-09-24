using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Costs.Queries.GetCompanyCosts;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The company's cost is everything it spent, not only what is tied to a site.
/// Each test uses a period of its own so the shared database's other rows cannot
/// leak into the sums.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CompanyCostsTests : IntegrationTestBase
{
    public CompanyCostsTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Years before 2026: other tests seed rentals and rents that start in 2026 and
    // never end, which would count into any later year however empty it looks.
    private static int nextYear = 2000;

    /// <summary>A quiet year no other test writes into.</summary>
    private static (DateOnly From, DateOnly To) FreshYear()
    {
        var year = Interlocked.Increment(ref nextYear);

        return (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
    }

    private async Task<CompanyCostsDto> ReportAsync(DateOnly from, DateOnly to, UserRole role = UserRole.SuperAdmin)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, role);
            scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);

            return await scope.Send(new GetCompanyCostsQuery { From = from, To = to });
        });
    }

    [Fact]
    public async Task A_period_with_no_costs_is_zero_in_every_kind()
    {
        var (from, to) = FreshYear();

        var report = await ReportAsync(from, to);

        Assert.Equal(0m, report.Total);
        Assert.Equal(0m, report.Vehicles + report.Tools + report.GeneralExpenses + report.Material + report.Accommodation);
    }

    [Fact]
    public async Task Costs_count_whether_or_not_they_belong_to_a_project()
    {
        var (from, to) = FreshYear();
        var day = new DateOnly(from.Year, 3, 10);

        await InScope(async scope =>
        {
            var vehicle = await TestData.SeedVehicleAsync(scope);
            var project = await TestData.SeedProjectAsync(scope);

            scope.Db.VehicleExpenses.Add(new VehicleExpense
            {
                VehicleId = vehicle.Id,
                Kind = VehicleExpenseKind.Fuel,
                Amount = 80m,
                Litres = 40m,
                OccurredOn = day,
                Status = VehicleExpenseStatus.Approved,
            });

            // One tied to a project, one to none: both are the company's cost.
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping,
                Amount = 120m,
                OccurredOn = day,
                ProjectId = project.Id,
            });
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping,
                Amount = 30m,
                OccurredOn = day,
            });

            await scope.Db.SaveChangesAsync();
        });

        var report = await ReportAsync(from, to);

        Assert.Equal(80m, report.Vehicles);
        Assert.Equal(150m, report.GeneralExpenses);
        Assert.Equal(230m, report.Total);
    }

    [Fact]
    public async Task Priced_material_issued_out_counts_with_or_without_a_project()
    {
        var (from, to) = FreshYear();
        var day = new DateOnly(from.Year, 5, 2);

        await InScope(async scope =>
        {
            var material = new Material { Name = $"Kabl {Guid.NewGuid():N}"[..12], Unit = "m", UnitPrice = 2m };
            scope.Db.Materials.Add(material);
            await scope.Db.SaveChangesAsync();

            scope.Db.MaterialMovements.Add(new MaterialMovement
            {
                MaterialId = material.Id,
                Kind = MaterialMovementKind.Out,
                Quantity = 50m,
                UnitPrice = 2m,
                OccurredOn = day,
            });

            await scope.Db.SaveChangesAsync();
        });

        var report = await ReportAsync(from, to);

        Assert.Equal(100m, report.Material);
    }

    [Fact]
    public async Task A_rented_car_and_a_rented_tool_are_costs_of_the_company()
    {
        var (from, to) = FreshYear();

        await InScope(async scope =>
        {
            var vehicle = await TestData.SeedVehicleAsync(scope);

            scope.Db.VehicleRentalRates.Add(new VehicleRentalRate
            {
                VehicleId = vehicle.Id,
                MonthlyAmount = 300m,
                StartDate = new DateOnly(from.Year, 1, 1),
                EndDate = new DateOnly(from.Year, 1, 30),
            });

            await scope.Db.SaveChangesAsync();
        });

        var report = await ReportAsync(from, to);

        // 30 of 30 days at 300/month.
        Assert.Equal(300m, report.Vehicles);
    }

    [Fact]
    public async Task Housing_rent_counts_once_for_the_whole_accommodation_project_or_not()
    {
        var (from, to) = FreshYear();

        await InScope(async scope =>
        {
            var flat = await TestData.SeedAccommodationAsync(scope);

            scope.Db.AccommodationRates.Add(new AccommodationRate
            {
                AccommodationId = flat.Id,
                Amount = 600m,
                Kind = AccommodationChargeKind.OneOff,
                StartDate = new DateOnly(from.Year, 6, 1),
            });

            await scope.Db.SaveChangesAsync();
        });

        var report = await ReportAsync(from, to);

        Assert.Equal(600m, report.Accommodation);
    }

    [Fact]
    public async Task Below_project_manager_the_pay_half_is_hidden_but_the_rest_is_not()
    {
        var (from, to) = FreshYear();
        var day = new DateOnly(from.Year, 4, 4);

        await InScope(async scope =>
        {
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping,
                Amount = 40m,
                OccurredOn = day,
            });

            await scope.Db.SaveChangesAsync();
        });

        // The Foreman role may record spending but not see pay.
        var report = await ReportAsync(from, to, UserRole.Foreman);

        Assert.False(report.IncludesLabour);
        Assert.Equal(0m, report.Labour);
        Assert.Equal(0m, report.ManualPay);
        Assert.Equal(40m, report.GeneralExpenses);
    }

    [Fact]
    public async Task A_worker_may_not_see_company_costs()
    {
        var (from, to) = FreshYear();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => ReportAsync(from, to, UserRole.Worker));
    }
}
