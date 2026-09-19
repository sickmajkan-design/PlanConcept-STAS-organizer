using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Accommodations.Costs;

public class EmployeeCostShare
{
    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public int PersonDays { get; init; }

    public decimal Cost { get; init; }
}

public class ProjectCostShare
{
    /// <summary>Null for housing not charged to any project.</summary>
    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public decimal Cost { get; init; }
}

public class AccommodationCostSummaryDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public int Days { get; init; }

    public decimal Total { get; init; }

    /// <summary>The share of the monthly rent that falls in the period, including the days nobody was there.</summary>
    public decimal MonthlyPortion { get; init; }

    public decimal DailyPortion { get; init; }

    public decimal OneOffPortion { get; init; }

    /// <summary>Monthly rent for days when the place stood empty: paid for, used by nobody.</summary>
    public decimal VacancyCost { get; init; }

    public int VacantDays { get; init; }

    public int OccupiedPersonDays { get; init; }

    public IReadOnlyList<EmployeeCostShare> ByEmployee { get; init; } = [];

    public IReadOnlyList<ProjectCostShare> ByProject { get; init; } = [];
}

/// <summary>
/// Works out what an accommodation cost over a stretch of days and who it was for.
/// </summary>
/// <remarks>
/// A day at a time, because that is the only unit all three kinds of charge
/// share. A monthly rent is spread over the days of its own month (so February
/// costs a little more per day than March), and split equally among whoever
/// slept there that day. A day nobody slept there is a vacancy: the rent was
/// paid and nobody used it, which is worth seeing rather than hiding in a
/// person's share. A per-person daily rate only counts the nights somebody
/// stayed. A one-off charge lands on its own date and belongs to the
/// accommodation, not to any one person.
/// </remarks>
public static class AccommodationCostCalculator
{
    public const int MaxDays = 3660;

    public static AccommodationCostSummaryDto Calculate(
        IReadOnlyCollection<AccommodationRate> rates,
        IReadOnlyCollection<AccommodationStay> stays,
        DateOnly from,
        DateOnly to,
        IReadOnlyDictionary<Guid, string> employeeNames,
        IReadOnlyDictionary<Guid, string> projectNames)
    {
        var monthlyPortion = 0m;
        var dailyPortion = 0m;
        var oneOffPortion = 0m;
        var vacancyCost = 0m;
        var vacantDays = 0;
        var personDays = 0;

        var employeeCost = new Dictionary<Guid, decimal>();
        var employeeDays = new Dictionary<Guid, int>();
        // Guid.Empty stands for "not charged to any project": a dictionary cannot key on null.
        var projectCost = new Dictionary<Guid, decimal>();

        var days = to.DayNumber - from.DayNumber + 1;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var occupants = stays.Where(s => s.CoversDay(day)).ToList();

            var monthly = rates
                .Where(r => r.Kind == AccommodationChargeKind.Monthly && r.CoversDay(day))
                .Sum(r => r.Amount / DateTime.DaysInMonth(day.Year, day.Month));

            var daily = rates
                .Where(r => r.Kind == AccommodationChargeKind.DailyPerPerson && r.CoversDay(day))
                .Sum(r => r.Amount);

            oneOffPortion += rates
                .Where(r => r.Kind == AccommodationChargeKind.OneOff && r.StartDate == day)
                .Sum(r => r.Amount);

            monthlyPortion += monthly;

            if (occupants.Count == 0)
            {
                vacantDays++;
                vacancyCost += monthly;
                continue;
            }

            personDays += occupants.Count;
            dailyPortion += daily * occupants.Count;

            var eachShare = (monthly / occupants.Count) + daily;

            foreach (var stay in occupants)
            {
                employeeCost[stay.EmployeeId] = employeeCost.GetValueOrDefault(stay.EmployeeId) + eachShare;
                employeeDays[stay.EmployeeId] = employeeDays.GetValueOrDefault(stay.EmployeeId) + 1;
                var projectKey = stay.ProjectId ?? Guid.Empty;
                projectCost[projectKey] = projectCost.GetValueOrDefault(projectKey) + eachShare;
            }
        }

        return new AccommodationCostSummaryDto
        {
            From = from,
            To = to,
            Days = days,
            Total = Math.Round(monthlyPortion + dailyPortion + oneOffPortion, 2),
            MonthlyPortion = Math.Round(monthlyPortion, 2),
            DailyPortion = Math.Round(dailyPortion, 2),
            OneOffPortion = Math.Round(oneOffPortion, 2),
            VacancyCost = Math.Round(vacancyCost, 2),
            VacantDays = vacantDays,
            OccupiedPersonDays = personDays,
            ByEmployee = employeeCost
                .Select(e => new EmployeeCostShare
                {
                    EmployeeId = e.Key,
                    EmployeeName = employeeNames.GetValueOrDefault(e.Key, "?"),
                    PersonDays = employeeDays[e.Key],
                    Cost = Math.Round(e.Value, 2)
                })
                .OrderByDescending(e => e.Cost)
                .ToList(),
            ByProject = projectCost
                .Select(p => new ProjectCostShare
                {
                    ProjectId = p.Key == Guid.Empty ? null : p.Key,
                    ProjectName = p.Key == Guid.Empty ? null : projectNames.GetValueOrDefault(p.Key),
                    Cost = Math.Round(p.Value, 2)
                })
                .OrderByDescending(p => p.Cost)
                .ToList()
        };
    }
}
