using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Planning;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class PlannedLabourCostTests : IntegrationTestBase
{
    public PlannedLabourCostTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Monday 2031-09-01 to Sunday 2031-09-07.
    private static readonly DateOnly Monday = new(2031, 9, 1);

    private async Task<(Employee Worker, Project Site)> PostedAsync(Action<EmployeeRate>? rate, string? country = null)
    {
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope, countryCode: country));

        await InScopeAs(UserRole.Admin, scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = worker.Id, ProjectId = site.Id, From = Monday, To = Monday.AddDays(6) }));

        if (rate is not null)
        {
            await InScope(async scope =>
            {
                var row = new EmployeeRate { EmployeeId = worker.Id, StartDate = new DateOnly(2030, 1, 1) };
                rate(row);
                scope.Db.EmployeeRates.Add(row);
                await scope.Db.SaveChangesAsync();
            });
        }

        return (worker, site);
    }

    private Task<PlannedLabourCostDto> CostAsync(UserRole role = UserRole.SuperAdmin, decimal hours = 8m) =>
        InScopeAs(role, scope => scope.Send(new GetPlannedLabourCostQuery { From = Monday, To = Monday.AddDays(6), HoursPerDay = hours }));

    private static PlannedLabourCostMonthDto Month(PlannedLabourCostDto dto, Guid projectId) =>
        dto.Projects.Single(p => p.ProjectId == projectId).Months.Single();

    [Fact]
    public async Task An_hourly_worker_costs_the_assumed_hours_for_each_working_day_posted_and_nothing_for_the_weekend()
    {
        var (_, site) = await PostedAsync(r => { r.RateType = RateType.Hourly; r.HourlyRate = 10m; });

        var cost = await CostAsync();

        var month = Month(cost, site.Id);
        Assert.Equal("2031-09", month.Month);
        Assert.Equal(5, month.PlannedDays);
        Assert.Equal(400m, month.PlannedCost);
    }

    [Fact]
    public async Task Leave_takes_a_day_out_and_the_assumed_hours_change_the_figure()
    {
        var (worker, site) = await PostedAsync(r => { r.RateType = RateType.Hourly; r.HourlyRate = 10m; });
        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence { EmployeeId = worker.Id, Type = AbsenceType.AnnualLeave, Status = AbsenceStatus.Approved, StartDate = Monday.AddDays(2), EndDate = Monday.AddDays(2) });
            await scope.Db.SaveChangesAsync();
        });

        var cost = await CostAsync(hours: 6m);

        var month = Month(cost, site.Id);
        Assert.Equal(4, month.PlannedDays);
        Assert.Equal(240m, month.PlannedCost);
    }

    [Fact]
    public async Task A_daily_rate_is_one_amount_per_day_whatever_the_hours()
    {
        var (_, site) = await PostedAsync(r => { r.RateType = RateType.Daily; r.DailyRate = 90m; });

        Assert.Equal(450m, Month(await CostAsync(hours: 3m), site.Id).PlannedCost);
    }

    [Fact]
    public async Task A_public_holiday_of_the_site_country_is_not_a_working_day()
    {
        await InScope(async scope =>
        {
            if (!await scope.Db.PublicHolidays.AnyAsync(h => h.Date == Monday.AddDays(1) && h.CountryCode == "HR"))
            {
                scope.Db.PublicHolidays.Add(new PublicHoliday { Date = Monday.AddDays(1), Name = "Test holiday", CountryCode = "HR" });
                await scope.Db.SaveChangesAsync();
            }
        });
        var (_, site) = await PostedAsync(r => { r.RateType = RateType.Hourly; r.HourlyRate = 10m; }, country: "HR");

        var month = Month(await CostAsync(), site.Id);

        Assert.Equal(4, month.PlannedDays);
    }

    [Fact]
    public async Task Somebody_with_no_rate_is_counted_as_unpriced_days_and_costs_nothing()
    {
        var before = (await CostAsync()).UnpricedDays;
        var (_, site) = await PostedAsync(null);

        var cost = await CostAsync();

        Assert.Equal(before + 5, cost.UnpricedDays);
        Assert.Equal(0m, Month(cost, site.Id).PlannedCost);
        Assert.Equal(5, Month(cost, site.Id).PlannedDays);
    }

    [Fact]
    public async Task Pay_is_not_shown_to_somebody_without_the_full_finance_grant()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => CostAsync(UserRole.Admin));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => CostAsync(UserRole.ProjectManager));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => CostAsync(UserRole.Foreman));
    }

    [Fact]
    public async Task An_administrator_with_the_full_finance_grant_may_see_it()
    {
        var admin = await InScope(async scope =>
        {
            var created = await TestData.SeedUserAsync(scope, UserRole.Admin);
            created.FinanceAccess = FinanceAccess.Full;
            await scope.Db.SaveChangesAsync();
            return created;
        });

        var cost = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, UserRole.Admin, null, admin.Email);
            return scope.Send(new GetPlannedLabourCostQuery { From = Monday, To = Monday.AddDays(6) });
        });

        Assert.Equal(8m, cost.HoursPerDay);
    }

    [Fact]
    public async Task An_unreasonable_working_day_is_refused()
    {
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(() => CostAsync(hours: 30m));
    }
}
