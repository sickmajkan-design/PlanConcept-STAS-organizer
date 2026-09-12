using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Costs.Commands.DeleteCostRecord;
using Construction.Application.Features.Costs.Commands.RecordMaterialMovement;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Costs.Commands.SetEmployeeRate;
using Construction.Application.Features.Costs.Queries.GetCostRecords;
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
