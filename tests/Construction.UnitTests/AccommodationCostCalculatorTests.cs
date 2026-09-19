using Construction.Application.Features.Accommodations.Costs;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.UnitTests;

public class AccommodationCostCalculatorTests
{
    private static readonly DateOnly March1 = new(2026, 3, 1);
    private static readonly DateOnly March31 = new(2026, 3, 31);

    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Ivo = Guid.NewGuid();

    private static readonly Dictionary<Guid, string> Names = new() { [Ana] = "Ana", [Ivo] = "Ivo" };
    private static readonly Dictionary<Guid, string> NoProjects = new();

    private static AccommodationRate Monthly(decimal amount, DateOnly start, DateOnly? end = null) =>
        new() { Kind = AccommodationChargeKind.Monthly, Amount = amount, StartDate = start, EndDate = end };

    private static AccommodationRate Daily(decimal amount, DateOnly start, DateOnly? end = null) =>
        new() { Kind = AccommodationChargeKind.DailyPerPerson, Amount = amount, StartDate = start, EndDate = end };

    private static AccommodationRate OneOff(decimal amount, DateOnly day) =>
        new() { Kind = AccommodationChargeKind.OneOff, Amount = amount, StartDate = day, EndDate = day };

    private static AccommodationStay Stay(Guid who, DateOnly start, DateOnly? end = null, Guid? project = null) =>
        new() { EmployeeId = who, StartDate = start, EndDate = end, ProjectId = project };

    [Fact]
    public void A_full_month_of_rent_costs_the_rent()
    {
        var result = AccommodationCostCalculator.Calculate(
            [Monthly(310m, March1)], [Stay(Ana, March1)], March1, March31, Names, NoProjects);

        Assert.Equal(310m, result.Total);
        Assert.Equal(310m, result.MonthlyPortion);
        Assert.Equal(0m, result.VacancyCost);
        Assert.Equal(31, result.OccupiedPersonDays);
    }

    [Fact]
    public void Two_people_share_the_rent_equally_for_the_days_they_overlap()
    {
        var result = AccommodationCostCalculator.Calculate(
            [Monthly(310m, March1)],
            [Stay(Ana, March1), Stay(Ivo, March1)],
            March1,
            March31,
            Names,
            NoProjects);

        Assert.Equal(310m, result.Total);
        Assert.Equal(2, result.ByEmployee.Count);
        Assert.All(result.ByEmployee, e => Assert.Equal(155m, e.Cost));
    }

    [Fact]
    public void Days_nobody_sleeps_there_are_vacancy_not_somebodys_share()
    {
        // Ana only from the 11th: ten empty days at 10 a day.
        var result = AccommodationCostCalculator.Calculate(
            [Monthly(310m, March1)],
            [Stay(Ana, new DateOnly(2026, 3, 11))],
            March1,
            March31,
            Names,
            NoProjects);

        Assert.Equal(310m, result.Total);
        Assert.Equal(100m, result.VacancyCost);
        Assert.Equal(10, result.VacantDays);
        Assert.Equal(210m, Assert.Single(result.ByEmployee).Cost);
    }

    [Fact]
    public void A_per_person_daily_rate_only_counts_the_nights_somebody_stays()
    {
        var result = AccommodationCostCalculator.Calculate(
            [Daily(20m, March1)],
            [Stay(Ana, March1, new DateOnly(2026, 3, 3)), Stay(Ivo, March1, new DateOnly(2026, 3, 2))],
            March1,
            March31,
            Names,
            NoProjects);

        // Ana 3 nights, Ivo 2 nights.
        Assert.Equal(100m, result.DailyPortion);
        Assert.Equal(100m, result.Total);
        Assert.Equal(5, result.OccupiedPersonDays);
    }

    [Fact]
    public void A_one_off_charge_lands_on_its_date_and_belongs_to_nobody_in_particular()
    {
        var result = AccommodationCostCalculator.Calculate(
            [OneOff(500m, new DateOnly(2026, 3, 15))], [Stay(Ana, March1)], March1, March31, Names, NoProjects);

        Assert.Equal(500m, result.OneOffPortion);
        Assert.Equal(500m, result.Total);
        Assert.Equal(0m, Assert.Single(result.ByEmployee).Cost);
    }

    [Fact]
    public void A_one_off_outside_the_period_is_left_out()
    {
        var result = AccommodationCostCalculator.Calculate(
            [OneOff(500m, new DateOnly(2026, 4, 2))], [], March1, March31, Names, NoProjects);

        Assert.Equal(0m, result.Total);
    }

    [Fact]
    public void The_cost_follows_the_project_each_stay_is_charged_to()
    {
        var site = Guid.NewGuid();

        var result = AccommodationCostCalculator.Calculate(
            [Monthly(310m, March1)],
            [Stay(Ana, March1, project: site), Stay(Ivo, March1)],
            March1,
            March31,
            Names,
            new Dictionary<Guid, string> { [site] = "Vidikovac" });

        Assert.Equal(2, result.ByProject.Count);
        Assert.Equal(155m, result.ByProject.Single(p => p.ProjectId == site).Cost);
        Assert.Equal(155m, result.ByProject.Single(p => p.ProjectId is null).Cost);
    }

    [Fact]
    public void A_rent_that_changes_mid_month_is_worked_out_day_by_day()
    {
        // 310 for the first half of March, 620 for the second: 16 days of 10 and 15 days of 20.
        var result = AccommodationCostCalculator.Calculate(
            [
                Monthly(310m, March1, new DateOnly(2026, 3, 16)),
                Monthly(620m, new DateOnly(2026, 3, 17))
            ],
            [Stay(Ana, March1)],
            March1,
            March31,
            Names,
            NoProjects);

        Assert.Equal(16 * 10m + 15 * 20m, result.Total);
    }
}
