using Construction.Application.Features.Costs.Commands.ReopenVehicleExpense;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Costs.Commands.DeleteCostRecord;
using Construction.Application.Features.Costs.Commands.RecordMaterialMovement;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Costs.Commands.ReviewVehicleExpense;
using Construction.Application.Features.Costs.Commands.SetEmployeeRate;
using Construction.Application.Features.Costs.Commands.UpdateVehicleExpense;
using Construction.Application.Features.Costs.Queries.GetCostRecords;
using Construction.Application.Features.Costs.Queries.GetFuelConsumptionFlags;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Construction.Application.Features.Materials.Commands.AdjustMaterialQuantity;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Costing runs against PostgreSQL because the numbers are the product: an
/// arithmetic slip here is a wrong price on a real job, and it will look
/// entirely plausible on the screen.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CostTests : IntegrationTestBase
{
    public CostTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly March = new(2026, 3, 2);
    private static readonly DateOnly June = new(2026, 6, 1);

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    // ---- rates -----------------------------------------------------------

    [Fact]
    public async Task A_raise_closes_off_the_rate_before_it()
    {
        // The office says "from June he costs 900". They should not also have
        // to remember to end the old one.
        var (employee, admin) = await SeedRateSetterAsync();

        await SetRateAsync(admin, employee.Id, 800m, March);
        await SetRateAsync(admin, employee.Id, 900m, June);

        var rates = await InScope(scope => scope.Db.EmployeeRates
            .Where(r => r.EmployeeId == employee.Id)
            .OrderBy(r => r.StartDate)
            .ToListAsync());

        Assert.Equal(2, rates.Count);
        Assert.Equal(June.AddDays(-1), rates[0].EndDate);
        Assert.Null(rates[1].EndDate);
    }

    [Fact]
    public async Task A_backdated_rate_landing_inside_a_priced_period_is_refused()
    {
        // March is already priced. Quietly repricing it would change what a
        // finished job is recorded as having cost.
        var (employee, admin) = await SeedRateSetterAsync();

        await SetRateAsync(admin, employee.Id, 800m, March, March.AddMonths(2));

        await Assert.ThrowsAsync<ConflictException>(() =>
            SetRateAsync(admin, employee.Id, 850m, March.AddDays(10), March.AddDays(20)));
    }

    [Fact]
    public async Task A_foreman_cannot_set_a_rate()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            SetRateAsync(foreman, employee.Id, 800m, March));
    }

    [Fact]
    public async Task A_foreman_cannot_read_pay_rates_at_all()
    {
        // Refused rather than narrowed, unlike everything else in the system:
        // there is no useful subset of a colleague's pay.
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetEmployeeRatesQuery());
        }));
    }

    // ---- stock movements -------------------------------------------------

    [Fact]
    public async Task A_delivery_raises_the_stock_and_an_issue_lowers_it()
    {
        var (material, foreman) = await SeedStockKeeperAsync(0m);
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await RecordMovementAsync(foreman, material.Id, MaterialMovementKind.In, 1000m, 20m);
        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 250m, projectId: project.Id);

        var quantity = await InScope(scope => scope.Db.Materials
            .Where(m => m.Id == material.Id)
            .Select(m => m.Quantity)
            .SingleAsync());

        Assert.Equal(750m, quantity);
    }

    [Fact]
    public async Task Issuing_more_than_is_on_the_shelf_is_refused()
    {
        var (material, foreman) = await SeedStockKeeperAsync(100m);
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await Assert.ThrowsAsync<ConflictException>(() => RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 101m, projectId: project.Id));

        // And the refusal left nothing behind — the movement and the total
        // move together or not at all.
        var movements = await InScope(scope =>
            scope.Db.MaterialMovements.CountAsync(m => m.MaterialId == material.Id));

        Assert.Equal(0, movements);
    }

    [Fact]
    public async Task An_issue_is_valued_at_the_average_of_what_was_bought()
    {
        // 100 at 10 and 100 at 20 makes 15, and the issue keeps that number
        // even if the next delivery arrives at a different price.
        var (material, foreman) = await SeedStockKeeperAsync(0m);
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await RecordMovementAsync(foreman, material.Id, MaterialMovementKind.In, 100m, 10m);
        await RecordMovementAsync(foreman, material.Id, MaterialMovementKind.In, 100m, 20m);

        var issue = await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 50m, projectId: project.Id);

        Assert.Equal(15m, issue.UnitPrice);

        await RecordMovementAsync(foreman, material.Id, MaterialMovementKind.In, 100m, 90m);

        var unchanged = await InScope(scope => scope.Db.MaterialMovements
            .Where(m => m.Id == issue.Id)
            .Select(m => m.UnitPrice)
            .SingleAsync());

        Assert.Equal(15m, unchanged);
    }

    [Fact]
    public async Task A_backdated_issue_is_only_averaged_against_deliveries_that_had_already_happened()
    {
        // Day 1: 10 at 10. Day 3: 10 more at 20. An issue backdated to day 2
        // must be priced at 10 — the day-3 delivery had not happened yet —
        // not at the 15 you get by averaging both deliveries regardless of
        // date.
        var (material, foreman) = await SeedStockKeeperAsync(0m);
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var day1 = new DateOnly(2026, 5, 1);
        var day2 = new DateOnly(2026, 5, 2);
        var day3 = new DateOnly(2026, 5, 3);

        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.In, 10m, 10m, occurredOn: day1);
        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.In, 10m, 20m, occurredOn: day3);

        var backdatedIssue = await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 5m,
            projectId: project.Id, occurredOn: day2);

        Assert.Equal(10m, backdatedIssue.UnitPrice);

        var laterIssue = await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 5m,
            projectId: project.Id, occurredOn: day3);

        // Dated on or after both deliveries: now both count.
        Assert.Equal(15m, laterIssue.UnitPrice);
    }

    [Fact]
    public async Task Editing_an_issues_date_across_a_delivery_recomputes_its_price()
    {
        var (material, foreman) = await SeedStockKeeperAsync(0m);
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var day1 = new DateOnly(2026, 5, 1);
        var day2 = new DateOnly(2026, 5, 2);
        var day3 = new DateOnly(2026, 5, 3);

        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.In, 10m, 10m, occurredOn: day1);
        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.In, 10m, 20m, occurredOn: day3);

        var issue = await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 5m,
            projectId: project.Id, occurredOn: day3);

        Assert.Equal(15m, issue.UnitPrice);

        // Pull the same issue's date back to before the second delivery: its
        // price must fall back to what was known at that earlier date.
        var repriced = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Application.Features.Costs.Commands.UpdateMaterialMovement
                .UpdateMaterialMovementCommand
            {
                Id = issue.Id,
                MaterialId = material.Id,
                Kind = MaterialMovementKind.Out,
                Quantity = 5m,
                ProjectId = project.Id,
                OccurredOn = day2
            });
        });

        Assert.Equal(10m, repriced.UnitPrice);

        // And moving it back onto day 3 restores the full-average price.
        var backOnDay3 = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Application.Features.Costs.Commands.UpdateMaterialMovement
                .UpdateMaterialMovementCommand
            {
                Id = issue.Id,
                MaterialId = material.Id,
                Kind = MaterialMovementKind.Out,
                Quantity = 5m,
                ProjectId = project.Id,
                OccurredOn = day3
            });
        });

        Assert.Equal(15m, backOnDay3.UnitPrice);
    }

    [Fact]
    public async Task Issuing_stock_without_saying_where_it_went_is_refused()
    {
        // Otherwise the material leaves the shelf and lands on no report.
        var (material, foreman) = await SeedStockKeeperAsync(500m);

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => RecordMovementAsync(foreman, material.Id, MaterialMovementKind.Out, 10m));
    }

    [Fact]
    public async Task A_correction_may_go_down_but_a_delivery_may_not()
    {
        var (material, foreman) = await SeedStockKeeperAsync(500m);

        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Adjustment, -20m);

        var quantity = await InScope(scope => scope.Db.Materials
            .Where(m => m.Id == material.Id)
            .Select(m => m.Quantity)
            .SingleAsync());

        Assert.Equal(480m, quantity);

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => RecordMovementAsync(foreman, material.Id, MaterialMovementKind.In, -5m));
    }

    [Fact]
    public async Task The_stock_screens_plus_minus_leaves_a_movement_behind()
    {
        // The running total and the history have to agree, or there is no way
        // to tell which of the two is wrong.
        var (material, _) = await SeedStockKeeperAsync(100m);
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new AdjustMaterialQuantityCommand
            {
                Id = material.Id,
                Change = -12m,
                Reason = "Prebrojano u magacinu"
            });
        });

        var movement = await InScope(scope => scope.Db.MaterialMovements
            .Where(m => m.MaterialId == material.Id)
            .SingleAsync());

        Assert.Equal(MaterialMovementKind.Adjustment, movement.Kind);
        Assert.Equal(-12m, movement.Quantity);
        // Not priced: a loss was not consumed by any site.
        Assert.Null(movement.UnitPrice);
    }

    [Fact]
    public async Task Removing_a_movement_puts_the_stock_back()
    {
        var (material, foreman) = await SeedStockKeeperAsync(0m);
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var delivery = await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.In, 300m, 5m);

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new DeleteMaterialMovementCommand(delivery.Id));
        });

        var quantity = await InScope(scope => scope.Db.Materials
            .Where(m => m.Id == material.Id)
            .Select(m => m.Quantity)
            .SingleAsync());

        Assert.Equal(0m, quantity);
    }

    [Fact]
    public async Task Undoing_a_delivery_that_has_since_been_used_is_refused()
    {
        // The shelf would go negative. A correction is the right fix, not a
        // rewrite of what happened.
        var (material, foreman) = await SeedStockKeeperAsync(0m);
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        var delivery = await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.In, 100m, 5m);

        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 80m, projectId: project.Id);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new DeleteMaterialMovementCommand(delivery.Id));
        }));
    }

    // ---- vehicle expenses ------------------------------------------------

    [Fact]
    public async Task Fuel_needs_litres_and_nothing_else_may_have_them()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 9000m));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => RecordExpenseAsync(
                foreman, vehicle.Id, VehicleExpenseKind.Insurance, 50000m, litres: 40m));
    }

    [Fact]
    public async Task A_fill_up_records_what_a_litre_cost()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Fuel, 10_000m, litres: 50m);

        Assert.Equal(200m, expense.PricePerLitre);
    }

    // ---- the reports -----------------------------------------------------

    [Fact]
    public async Task A_project_is_priced_at_the_rate_in_force_on_the_day()
    {
        // The whole reason rates are dated. Eight hours in March at 800 and
        // eight in June at 900 is 13600, not sixteen hours at whichever rate
        // happens to be current when the report is run.
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SetRateAsync(admin, employee.Id, 800m, March);
        await SetRateAsync(admin, employee.Id, 900m, June);

        await SeedApprovedShiftAsync(employee.Id, project.Id, March.AddDays(3), hours: 8);
        await SeedApprovedShiftAsync(employee.Id, project.Id, June.AddDays(3), hours: 8);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = June.AddMonths(1),
                ProjectId = project.Id
            });
        });

        var row = Assert.Single(report.Rows);

        Assert.Equal(960, row.LabourMinutes);
        Assert.Equal(13_600m, row.LabourCost);
        Assert.Equal(0, row.UnpricedMinutes);
    }

    [Fact]
    public async Task Hours_no_rate_covers_are_reported_rather_than_treated_as_free()
    {
        // A total that quietly omits somebody looks exactly like one that does
        // not, and the office would price the next job from it.
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SeedApprovedShiftAsync(employee.Id, project.Id, March.AddDays(3), hours: 8);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        var row = Assert.Single(report.Rows);

        Assert.Equal(0m, row.LabourCost);
        Assert.Equal(480, row.UnpricedMinutes);
    }

    [Fact]
    public async Task Only_approved_hours_are_a_cost()
    {
        // Unreviewed hours are a claim. A total that moved every time somebody
        // clocked out could not be used to price anything.
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SetRateAsync(admin, employee.Id, 800m, March);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, March.AddDays(3), hours: 8,
            status: TimeEntryStatus.Submitted);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Empty(report.Rows);
    }

    [Fact]
    public async Task A_break_is_not_paid_for()
    {
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SetRateAsync(admin, employee.Id, 600m, March);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, March.AddDays(3), hours: 8, breakMinutes: 30);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        var row = Assert.Single(report.Rows);

        Assert.Equal(450, row.LabourMinutes);
        Assert.Equal(4_500m, row.LabourCost);
    }

    // ---- weekend, holiday, overtime and travel premiums -------------------

    [Fact]
    public async Task A_shift_on_a_saturday_is_priced_at_the_weekend_rate()
    {
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var saturday = March.AddDays(5); // March 2 2026 is a Monday.

        await SetRateAsync(admin, employee.Id, 800m, March, weekendHourlyRate: 1_200m);
        await SeedApprovedShiftAsync(employee.Id, project.Id, saturday, hours: 8);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(9_600m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task A_shift_on_a_listed_public_holiday_is_priced_at_the_holiday_rate()
    {
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, countryCode: "BA"));
        var holiday = March.AddDays(1); // A Tuesday, deliberately not a weekend.

        await SeedPublicHolidayAsync(holiday, countryCode: "BA");
        await SetRateAsync(admin, employee.Id, 800m, March, holidayHourlyRate: 1_600m);
        await SeedApprovedShiftAsync(employee.Id, project.Id, holiday, hours: 8);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(12_800m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task A_holiday_in_one_country_does_not_price_a_shift_in_another()
    {
        // The whole reason the calendar is per country: this company runs
        // sites in more than one at once, and a Croatian holiday must not
        // give a German site's shift the holiday rate.
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, countryCode: "DE"));
        var holiday = March.AddDays(2);

        await SeedPublicHolidayAsync(holiday, countryCode: "BA");
        await SetRateAsync(admin, employee.Id, 800m, March, holidayHourlyRate: 1_600m);
        await SeedApprovedShiftAsync(employee.Id, project.Id, holiday, hours: 8);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(6_400m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task A_project_with_no_country_never_gets_the_holiday_rate()
    {
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope)); // No CountryCode.
        var holiday = March.AddDays(3);

        await SeedPublicHolidayAsync(holiday, countryCode: "BA");
        await SetRateAsync(admin, employee.Id, 800m, March, holidayHourlyRate: 1_600m);
        await SeedApprovedShiftAsync(employee.Id, project.Id, holiday, hours: 8);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(6_400m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task A_weekend_tag_on_a_weekday_shift_does_not_change_its_price()
    {
        // The calendar decides weekend/holiday pricing, not the tag someone
        // picked when logging the shift — a Monday priced as a weekend would
        // be wrong, whatever the entry says about itself.
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SetRateAsync(admin, employee.Id, 800m, March, weekendHourlyRate: 1_200m);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, March, hours: 8, workType: WorkType.Weekend);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(6_400m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task A_shift_tagged_overtime_is_priced_at_the_overtime_rate()
    {
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SetRateAsync(admin, employee.Id, 800m, March, overtimeHourlyRate: 1_500m);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, March, hours: 8, workType: WorkType.Overtime);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(12_000m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task A_shift_tagged_travel_is_priced_at_the_travel_rate()
    {
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SetRateAsync(admin, employee.Id, 800m, March, travelHourlyRate: 400m);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, March, hours: 8, workType: WorkType.Travel);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(3_200m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task An_unset_overtime_rate_falls_back_to_the_regular_rate()
    {
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await SetRateAsync(admin, employee.Id, 800m, March);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, March, hours: 8, workType: WorkType.Overtime);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(6_400m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task Overtime_on_a_weekend_is_priced_as_overtime_not_stacked_with_the_weekend_rate()
    {
        // One differently priced hour, not two premiums added together.
        var (employee, admin) = await SeedRateSetterAsync();
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var saturday = March.AddDays(5);

        await SetRateAsync(
            admin, employee.Id, 800m, March,
            weekendHourlyRate: 1_200m, overtimeHourlyRate: 1_500m);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, saturday, hours: 8, workType: WorkType.Overtime);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Equal(12_000m, Assert.Single(report.Rows).LabourCost);
    }

    [Fact]
    public async Task A_foreman_sees_the_material_half_and_not_the_labour()
    {
        // Withheld rather than the report refused, so the screen is still
        // useful to the person running the site.
        var (employee, admin) = await SeedRateSetterAsync();
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, 0m));

        await SetRateAsync(admin, employee.Id, 800m, March);
        await SeedApprovedShiftAsync(employee.Id, project.Id, March.AddDays(3), hours: 8);

        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.In, 100m, 30m, occurredOn: March);
        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Out, 10m,
            projectId: project.Id, occurredOn: March.AddDays(4));

        var asForeman = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.False(asForeman.IncludesLabour);
        Assert.Equal(0m, asForeman.TotalLabourCost);
        Assert.Equal(300m, asForeman.TotalMaterialCost);

        var asAdmin = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.True(asAdmin.IncludesLabour);
        Assert.Equal(6_400m, asAdmin.TotalLabourCost);
        Assert.Equal(300m, asAdmin.TotalMaterialCost);
    }

    [Fact]
    public async Task A_stock_correction_is_not_charged_to_any_site()
    {
        // Breakage is not consumption, and putting it on a project would make
        // that job look more expensive than it was.
        var (material, foreman) = await SeedStockKeeperAsync(0m);
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await RecordMovementAsync(foreman, material.Id, MaterialMovementKind.In, 100m, 30m);
        await RecordMovementAsync(
            foreman, material.Id, MaterialMovementKind.Adjustment, -40m,
            occurredOn: March.AddDays(4));

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = project.Id
            });
        });

        Assert.Empty(report.Rows);
    }

    [Fact]
    public async Task The_fleet_report_splits_fuel_from_the_rest_and_works_out_consumption()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Fuel, 10_000m,
            litres: 50m, odometerKm: 100_000, occurredOn: March);
        await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Fuel, 12_000m,
            litres: 60m, odometerKm: 101_000, occurredOn: March.AddDays(10));
        await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 28_000m,
            occurredOn: March.AddDays(12));
        await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Insurance, 50_000m,
            occurredOn: March.AddDays(14));

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetVehicleCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                VehicleId = vehicle.Id
            });
        });

        var row = Assert.Single(report.Rows);

        Assert.Equal(22_000m, row.FuelCost);
        Assert.Equal(28_000m, row.ServiceCost);
        Assert.Equal(50_000m, row.OtherCost);
        Assert.Equal(100_000m, row.Total);
        Assert.Equal(1_000, row.DistanceKm);
        // 110 litres over 1000 km.
        Assert.Equal(11m, row.LitresPer100Km);
    }

    [Fact]
    public async Task Consumption_is_left_out_when_one_fill_up_cannot_show_it()
    {
        // A single reading gives no distance, and dividing by nothing would
        // put a headline figure on the screen built from one data point.
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Fuel, 10_000m,
            litres: 50m, odometerKm: 100_000, occurredOn: March);

        var report = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetVehicleCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                VehicleId = vehicle.Id
            });
        });

        var row = Assert.Single(report.Rows);

        Assert.Null(row.DistanceKm);
        Assert.Null(row.LitresPer100Km);
    }

    // ---- fuel consumption flags -------------------------------------------

    [Fact]
    public async Task A_fill_up_far_off_its_own_average_gets_flagged()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        // Three steady fill-ups at 10 L/100km establish the baseline the
        // fourth one is judged against.
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 100_000, occurredOn: March);
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 100_500, occurredOn: March.AddDays(5));
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 101_000, occurredOn: March.AddDays(10));
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 101_500, occurredOn: March.AddDays(15));
        // The odd one out: 100 litres over the same 500 km the others took
        // 50 for — double the vehicle's own average.
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 10_000m,
            litres: 100m, odometerKm: 102_000, occurredOn: March.AddDays(20));

        var flags = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetFuelConsumptionFlagsQuery { VehicleId = vehicle.Id });
        });

        var flag = Assert.Single(flags);

        Assert.Equal(500, flag.DistanceKm);
        Assert.Equal(20m, flag.LitresPer100Km);
        Assert.Equal(10m, flag.VehicleAverageLitresPer100Km);
        Assert.Equal(100m, flag.DeviationPercent);
    }

    [Fact]
    public async Task Nothing_is_flagged_until_the_vehicle_has_a_baseline()
    {
        // Two wildly different fill-ups with no baseline yet — flagging either
        // one would just be "compared to the one before it", which is not a
        // baseline, it's a coin flip.
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 100_000, occurredOn: March);
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 20_000m,
            litres: 200m, odometerKm: 100_500, occurredOn: March.AddDays(5));

        var flags = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetFuelConsumptionFlagsQuery { VehicleId = vehicle.Id });
        });

        Assert.Empty(flags);
    }

    [Fact]
    public async Task A_reset_odometer_is_skipped_rather_than_flagged()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 100_000, occurredOn: March);
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 100_500, occurredOn: March.AddDays(5));
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 101_000, occurredOn: March.AddDays(10));
        // A cluster replacement, or a typo — the reading goes backwards.
        await RecordExpenseAsync(foreman, vehicle.Id, VehicleExpenseKind.Fuel, 5_000m,
            litres: 50m, odometerKm: 500, occurredOn: March.AddDays(15));

        var flags = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetFuelConsumptionFlagsQuery { VehicleId = vehicle.Id });
        });

        Assert.Empty(flags);
    }

    [Fact]
    public async Task A_worker_may_not_see_fuel_consumption_flags()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetFuelConsumptionFlagsQuery());
        }));
    }

    // ---- vehicle expense review --------------------------------------------

    [Fact]
    public async Task A_recorded_cost_starts_pending_and_can_be_approved()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        Assert.Equal(VehicleExpenseStatus.Pending, expense.Status);

        var reviewed = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        });

        Assert.Equal(VehicleExpenseStatus.Approved, reviewed.Status);
        Assert.Null(reviewed.ReviewNote);
        Assert.Equal(reviewer.Email, reviewed.ReviewedByName);
    }

    [Fact]
    public async Task Rejecting_a_cost_requires_a_reason()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        var reviewed = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand
            {
                Id = expense.Id,
                Approve = false,
                Note = "Wrong vehicle billed."
            });
        });

        Assert.Equal(VehicleExpenseStatus.Rejected, reviewed.Status);
        Assert.Equal("Wrong vehicle billed.", reviewed.ReviewNote);
    }

    [Fact]
    public async Task A_foreman_cannot_review_a_cost_even_one_they_did_not_record()
    {
        // Review is office work, one tier above recording — the same split
        // TimeEntries uses between submitting hours and signing them off.
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var otherForeman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, otherForeman);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        }));
    }

    [Fact]
    public async Task Nobody_reviews_a_cost_they_recorded_themselves()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();

        // The recorder happens to also hold a reviewing role — a small team
        // where the same person wears both hats. The rule still has to hold.
        var expense = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new RecordVehicleExpenseCommand
            {
                VehicleId = vehicle.Id,
                Kind = VehicleExpenseKind.Service,
                Amount = 5_000m,
                OccurredOn = March
            });
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            // Promote them past Foreman for this call only, so the failure is
            // provably about self-review and not the role check above it.
            scope.CurrentUser.SignInAs(foreman.Id, UserRole.ProjectManager, null, foreman.Email);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        }));
    }

    [Fact]
    public async Task A_rejection_tells_whoever_recorded_the_cost_and_says_why()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand
            {
                Id = expense.Id,
                Approve = false,
                Note = "Receipt missing."
            });
        });

        var notification = await InScope(scope => scope.Db.Notifications
            .Where(n => n.UserId == foreman.Id && n.Type == NotificationType.VehicleExpenseRejected)
            .SingleAsync());

        Assert.Contains("Receipt missing.", notification.Body);
        var data = System.Text.Json.JsonDocument.Parse(notification.DataJson!).RootElement;

        Assert.Equal("Receipt missing.", data.GetProperty("note").GetString());
        Assert.Equal(expense.Id.ToString(), data.GetProperty("expenseId").GetString());
    }

    [Fact]
    public async Task An_approval_does_not_notify_anyone()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        });

        var count = await InScope(scope => scope.Db.Notifications
            .CountAsync(n => n.Type == NotificationType.VehicleExpenseRejected && n.UserId == foreman.Id));

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Recording_a_cost_tells_the_reviewers_but_not_the_person_who_recorded_it()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var manager = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        var recipients = await InScope(scope => scope.Db.Notifications
            .Where(n => n.Type == NotificationType.VehicleExpenseSubmitted)
            .Select(n => n.UserId)
            .ToListAsync());

        Assert.Contains(manager.Id, recipients);
        Assert.Contains(admin.Id, recipients);
        Assert.DoesNotContain(foreman.Id, recipients);

        var data = await InScope(scope => scope.Db.Notifications
            .Where(n => n.UserId == manager.Id && n.Type == NotificationType.VehicleExpenseSubmitted)
            .Select(n => n.DataJson)
            .SingleAsync());

        Assert.Equal(
            expense.Id.ToString(),
            System.Text.Json.JsonDocument.Parse(data!).RootElement.GetProperty("expenseId").GetString());
    }

    [Fact]
    public async Task Changing_an_approved_cost_puts_it_back_in_front_of_the_reviewers()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var manager = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await InScope(scope =>
        {
            ActAs(scope, manager);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        });

        var before = await InScope(scope => scope.Db.Notifications
            .CountAsync(n => n.UserId == manager.Id && n.Type == NotificationType.VehicleExpenseSubmitted));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new UpdateVehicleExpenseCommand
            {
                Id = expense.Id,
                VehicleId = vehicle.Id,
                Kind = VehicleExpenseKind.Service,
                Amount = 5_500m,
                OccurredOn = March
            });
        });

        var after = await InScope(scope => scope.Db.Notifications
            .CountAsync(n => n.UserId == manager.Id && n.Type == NotificationType.VehicleExpenseSubmitted));
        var adminHeardOwnEdit = await InScope(scope => scope.Db.Notifications
            .CountAsync(n => n.UserId == admin.Id &&
                             n.Type == NotificationType.VehicleExpenseSubmitted &&
                             n.Body.Contains("changed")));

        Assert.Equal(before + 1, after);
        Assert.Equal(0, adminHeardOwnEdit);
    }

    [Fact]
    public async Task A_decision_can_be_taken_back_and_the_cost_waits_for_review_again()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        });

        var reopened = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReopenVehicleExpenseCommand { Id = expense.Id });
        });

        Assert.Equal(VehicleExpenseStatus.Pending, reopened.Status);
        Assert.Null(reopened.ReviewNote);

        // Pending already: nothing to take back.
        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReopenVehicleExpenseCommand { Id = expense.Id });
        }));
    }

    [Fact]
    public async Task A_foreman_cannot_take_back_a_decision()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ReopenVehicleExpenseCommand { Id = expense.Id });
        }));
    }

    [Fact]
    public async Task A_new_material_with_an_invoice_starts_its_history_with_that_delivery()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var material = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Materials.Commands.CreateMaterial.CreateMaterialCommand
            {
                Name = "Cement CEM II",
                Unit = "vreća",
                Quantity = 40m,
                InvoiceNumber = "INV-2026-0142",
                Supplier = "Kastel d.o.o.",
                PurchaseUnitPrice = 9.5m,
                ReceivedOn = March
            });
        });

        var movement = await InScope(scope => scope.Db.MaterialMovements
            .SingleAsync(m => m.MaterialId == material.Id));

        Assert.Equal(MaterialMovementKind.In, movement.Kind);
        Assert.Equal(40m, movement.Quantity);
        Assert.Equal(9.5m, movement.UnitPrice);
        Assert.Equal("INV-2026-0142", movement.InvoiceNumber);
        Assert.Equal("Kastel d.o.o.", movement.Supplier);
        Assert.Equal(March, movement.OccurredOn);
    }

    [Fact]
    public async Task Starting_stock_without_an_invoice_is_an_unpriced_opening_balance()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var material = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Materials.Commands.CreateMaterial.CreateMaterialCommand
            {
                Name = "Šljunak",
                Unit = "m3",
                Quantity = 12m
            });
        });

        var movement = await InScope(scope => scope.Db.MaterialMovements
            .SingleAsync(m => m.MaterialId == material.Id));

        Assert.Equal(MaterialMovementKind.Adjustment, movement.Kind);
        Assert.Null(movement.UnitPrice);
        Assert.Null(movement.InvoiceNumber);
    }

    [Fact]
    public async Task A_supplier_or_price_without_an_invoice_number_is_refused()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Materials.Commands.CreateMaterial.CreateMaterialCommand
            {
                Name = "Armatura",
                Unit = "kg",
                Quantity = 100m,
                Supplier = "Kastel d.o.o."
            });
        }));
    }

    [Fact]
    public async Task Suppliers_already_used_are_offered_back_newest_first()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        foreach (var (supplier, invoice, day) in new[]
                 {
                     ("Stari dobavljač", "A-1", March),
                     ("Novi dobavljač", "B-1", March.AddDays(10))
                 })
        {
            await InScope(scope =>
            {
                ActAs(scope, admin);
                return scope.Send(new Construction.Application.Features.Materials.Commands.CreateMaterial.CreateMaterialCommand
                {
                    Name = "Materijal " + invoice,
                    Unit = "kom",
                    Quantity = 1m,
                    InvoiceNumber = invoice,
                    Supplier = supplier,
                    PurchaseUnitPrice = 1m,
                    ReceivedOn = day
                });
            });
        }

        var suppliers = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Costs.Queries.GetMaterialSuppliers.GetMaterialSuppliersQuery());
        });

        Assert.True(suppliers.ToList().IndexOf("Novi dobavljač") < suppliers.ToList().IndexOf("Stari dobavljač"));
    }

    [Fact]
    public async Task Falling_below_the_minimum_tells_the_office_once()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var manager = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        var material = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Materials.Commands.CreateMaterial.CreateMaterialCommand
            {
                Name = "Cement",
                Unit = "vreća",
                Quantity = 30m,
                MinimumQuantity = 20m
            });
        });

        async Task IssueAsync(decimal quantity) => await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new RecordMaterialMovementCommand
            {
                MaterialId = material.Id,
                Kind = MaterialMovementKind.Out,
                Quantity = quantity,
                ProjectId = project.Id
            });
        });

        Task<int> AlertsAsync() => InScope(scope => scope.Db.Notifications.CountAsync(
            n => n.UserId == manager.Id && n.Type == NotificationType.MaterialLowStock));

        await IssueAsync(5m);   // 25, still above the minimum
        Assert.Equal(0, await AlertsAsync());

        await IssueAsync(10m);  // 15, falls through it
        Assert.Equal(1, await AlertsAsync());

        await IssueAsync(5m);   // 10, already low: not announced again
        Assert.Equal(1, await AlertsAsync());
    }

    [Fact]
    public async Task Materials_below_their_minimum_can_be_listed()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        foreach (var (name, quantity, minimum) in new (string, decimal, decimal?)[]
                 {
                     ("Nisko", 3m, 10m),
                     ("Dovoljno", 30m, 10m),
                     ("Bez praga", 1m, null)
                 })
        {
            await InScope(scope =>
            {
                ActAs(scope, admin);
                return scope.Send(new Construction.Application.Features.Materials.Commands.CreateMaterial.CreateMaterialCommand
                {
                    Name = name,
                    Unit = "kom",
                    Quantity = quantity,
                    MinimumQuantity = minimum
                });
            });
        }

        var low = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Materials.Queries.GetMaterials.GetMaterialsQuery
            {
                LowStockOnly = true
            });
        });

        Assert.Contains(low.Items, m => m.Name == "Nisko");
        Assert.DoesNotContain(low.Items, m => m.Name == "Dovoljno");
        Assert.DoesNotContain(low.Items, m => m.Name == "Bez praga");
    }

    [Fact]
    public async Task Pricing_reports_the_last_and_the_quantity_weighted_average_purchase_price()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var material = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Materials.Commands.CreateMaterial.CreateMaterialCommand
            {
                Name = "Cement",
                Unit = "vreća",
                Quantity = 100m,
                InvoiceNumber = "A-1",
                Supplier = "Prvi",
                PurchaseUnitPrice = 10m,
                ReceivedOn = March
            });
        });

        await RecordMovementAsync(
            admin, material.Id, MaterialMovementKind.In, 300m, 20m,
            occurredOn: March.AddDays(10), invoiceNumber: "A-2");

        var pricing = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new Construction.Application.Features.Materials.Queries.GetMaterialPricing.GetMaterialPricingQuery(material.Id));
        });

        Assert.Equal(20m, pricing.LastPurchasePrice);
        Assert.Equal(March.AddDays(10), pricing.LastPurchasedOn);
        Assert.Equal(17.5m, pricing.AveragePurchasePrice);
        Assert.Equal(400m, pricing.TotalReceived);
    }

    [Fact]
    public async Task Someone_who_may_not_see_spending_cannot_read_what_a_material_cost()
    {
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, 10m));
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker);
            return scope.Send(new Construction.Application.Features.Materials.Queries.GetMaterialPricing.GetMaterialPricingQuery(material.Id));
        }));
    }

    [Fact]
    public async Task The_owner_may_approve_a_cost_they_recorded_themselves()
    {
        // Nobody sits above a SuperAdmin to send it to, and where one person
        // records most costs the general rule would make them unapprovable.
        var (vehicle, _) = await SeedFleetKeeperAsync();
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var expense = await RecordExpenseAsync(
            owner, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        var reviewed = await InScope(scope =>
        {
            ActAs(scope, owner);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        });

        Assert.Equal(VehicleExpenseStatus.Approved, reviewed.Status);
    }

    [Fact]
    public async Task An_admin_still_cannot_approve_their_own_cost()
    {
        // The exception is for the owner alone, not for the tier below.
        var (vehicle, _) = await SeedFleetKeeperAsync();
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var expense = await RecordExpenseAsync(
            admin, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        }));
    }

    [Fact]
    public async Task Editing_a_reviewed_cost_sends_it_back_to_pending()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        });

        var updated = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new UpdateVehicleExpenseCommand
            {
                Id = expense.Id,
                VehicleId = vehicle.Id,
                Kind = VehicleExpenseKind.Service,
                Amount = 6_000m,
                OccurredOn = March
            });
        });

        Assert.Equal(VehicleExpenseStatus.Pending, updated.Status);
        Assert.Null(updated.ReviewedByName);
    }

    [Fact]
    public async Task Reversing_a_rejection_needs_confirmation()
    {
        var (vehicle, foreman) = await SeedFleetKeeperAsync();
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var expense = await RecordExpenseAsync(
            foreman, vehicle.Id, VehicleExpenseKind.Service, 5_000m, occurredOn: March);

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand
            {
                Id = expense.Id,
                Approve = false,
                Note = "Needs a receipt."
            });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand { Id = expense.Id, Approve = true });
        }));

        var reversed = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewVehicleExpenseCommand
            {
                Id = expense.Id,
                Approve = true,
                Confirm = true
            });
        });

        Assert.Equal(VehicleExpenseStatus.Approved, reversed.Status);
    }

    [Fact]
    public async Task A_worker_gets_nowhere_near_any_of_it()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1)
            });
        }));
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<(Employee Employee, User Admin)> SeedRateSetterAsync()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        return (employee, admin);
    }

    private async Task<(Material Material, User Foreman)> SeedStockKeeperAsync(
        decimal quantity)
    {
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, quantity));
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman));

        return (material, foreman);
    }

    private async Task<(Vehicle Vehicle, User Foreman)> SeedFleetKeeperAsync()
    {
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman));

        return (vehicle, foreman);
    }

    private Task SetRateAsync(
        User actor,
        Guid employeeId,
        decimal hourlyRate,
        DateOnly startDate,
        DateOnly? endDate = null,
        decimal? weekendHourlyRate = null,
        decimal? holidayHourlyRate = null,
        decimal? overtimeHourlyRate = null,
        decimal? travelHourlyRate = null) =>
        InScope(scope =>
        {
            ActAs(scope, actor);
            return scope.Send(new SetEmployeeRateCommand
            {
                EmployeeId = employeeId,
                HourlyRate = hourlyRate,
                WeekendHourlyRate = weekendHourlyRate,
                HolidayHourlyRate = holidayHourlyRate,
                OvertimeHourlyRate = overtimeHourlyRate,
                TravelHourlyRate = travelHourlyRate,
                StartDate = startDate,
                EndDate = endDate
            });
        });

    private Task<Application.Features.Costs.Models.MaterialMovementDto> RecordMovementAsync(
        User actor,
        Guid materialId,
        MaterialMovementKind kind,
        decimal quantity,
        decimal? unitPrice = null,
        Guid? projectId = null,
        DateOnly? occurredOn = null,
        string? invoiceNumber = null) =>
        InScope(scope =>
        {
            ActAs(scope, actor);
            return scope.Send(new RecordMaterialMovementCommand
            {
                MaterialId = materialId,
                Kind = kind,
                Quantity = quantity,
                UnitPrice = unitPrice,
                ProjectId = projectId,
                OccurredOn = occurredOn,
                InvoiceNumber = invoiceNumber
                    ?? (kind == MaterialMovementKind.In ? $"INV-{Guid.NewGuid():N}" : null)
            });
        });

    private Task<Application.Features.Costs.Models.VehicleExpenseDto> RecordExpenseAsync(
        User actor,
        Guid vehicleId,
        VehicleExpenseKind kind,
        decimal amount,
        decimal? litres = null,
        int? odometerKm = null,
        DateOnly? occurredOn = null) =>
        InScope(scope =>
        {
            ActAs(scope, actor);
            return scope.Send(new RecordVehicleExpenseCommand
            {
                VehicleId = vehicleId,
                Kind = kind,
                Amount = amount,
                Litres = litres,
                OdometerKm = odometerKm,
                OccurredOn = occurredOn
            });
        });

    /// <summary>
    /// A finished, reviewed shift on a given day. Written straight to the
    /// database rather than through the clock-in commands, which refuse to
    /// backdate this far — the point here is the pricing, not the timesheet
    /// rules those commands already have their own tests for.
    /// </summary>
    private Task SeedApprovedShiftAsync(
        Guid employeeId,
        Guid projectId,
        DateOnly day,
        int hours,
        int breakMinutes = 0,
        TimeEntryStatus status = TimeEntryStatus.Approved,
        WorkType workType = WorkType.Regular) =>
        InScope(async scope =>
        {
            var startedAt = day.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc);

            scope.Db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = employeeId,
                ProjectId = projectId,
                StartedAt = startedAt,
                EndedAt = startedAt.AddHours(hours),
                BreakMinutes = breakMinutes,
                WorkType = workType,
                Status = status
            });

            await scope.Db.SaveChangesAsync();
        });

    private Task SeedPublicHolidayAsync(DateOnly date, string countryCode = "BA") =>
        InScope(async scope =>
        {
            scope.Db.PublicHolidays.Add(new PublicHoliday
            {
                Date = date,
                Name = "Test holiday",
                CountryCode = countryCode,
            });
            await scope.Db.SaveChangesAsync();
        });
}
