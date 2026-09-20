using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetProjectCosts;

/// <summary>One priced piece of approved labour: who, where, how long, what it cost.</summary>
public sealed record PricedLabourEntry(
    Guid EmployeeId,
    Guid ProjectId,
    int Minutes,
    decimal Cost,
    int UnpricedMinutes);

/// <summary>
/// Approved hours per site, priced by the rate in force on the day — weekend
/// and public-holiday hours at that rate's own premium, when it sets one; a
/// subcontractor on a daily rate instead earns one flat amount per day worked,
/// whatever the hours.
/// </summary>
/// <remarks>
/// The one place labour is priced, so the per-site totals and the per-person
/// breakdown of a site can never disagree.
///
/// The covering rate is found per entry with a correlated subquery rather than
/// loading every rate and matching in memory, so the work stays in the
/// database where the index is. Which price applies is decided afterwards, in
/// memory, because that decision needs the holiday calendar, a plain
/// <c>DayOfWeek</c> check, and — for daily rates — grouping entries by
/// employee and day. An entry no rate covers still contributes its minutes to
/// <see cref="PricedLabourEntry.UnpricedMinutes"/> and nothing to the cost —
/// reported, not silently free.
///
/// The holiday calendar is per country: a date only counts as a holiday for a
/// shift whose project's country matches the calendar row's own. A project
/// with no country set never gets the holiday rate.
///
/// The day is taken from the shift's start in UTC. A shift beginning after
/// midnight local time therefore prices against the previous day, which only
/// matters right at a boundary.
/// </remarks>
public static class ProjectLabourPricing
{
    public static async Task<List<PricedLabourEntry>> LoadAsync(
        IApplicationDbContext context,
        DateOnly from,
        DateOnly to,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var holidays = (await context.PublicHolidays
                .AsNoTracking()
                .Where(h => h.Date >= from && h.Date <= to)
                .Select(h => new { h.CountryCode, h.Date })
                .ToListAsync(cancellationToken))
            .Select(h => (h.CountryCode, h.Date))
            .ToHashSet();

        var priced = await context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Status == TimeEntryStatus.Approved
                && t.ProjectId != null
                && t.EndedAt != null)
            .Where(t => projectId == null || t.ProjectId == projectId)
            .Where(t => DateOnly.FromDateTime(t.StartedAt) >= from
                && DateOnly.FromDateTime(t.StartedAt) <= to)
            .Select(t => new
            {
                t.EmployeeId,
                ProjectId = t.ProjectId!.Value,
                ProjectCountryCode = t.Project!.CountryCode,
                Day = DateOnly.FromDateTime(t.StartedAt),
                t.WorkType,
                Minutes = (int)((t.EndedAt!.Value - t.StartedAt).TotalMinutes
                    - t.BreakMinutes),
                Rate = context.EmployeeRates
                    .Where(r => r.EmployeeId == t.EmployeeId
                        && r.StartDate <= DateOnly.FromDateTime(t.StartedAt)
                        && (r.EndDate == null || r.EndDate >= DateOnly.FromDateTime(t.StartedAt)))
                    .Select(r => new
                    {
                        r.RateType,
                        r.HourlyRate,
                        r.WeekendHourlyRate,
                        r.HolidayHourlyRate,
                        r.OvertimeHourlyRate,
                        r.TravelHourlyRate,
                        r.DailyRate
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        // Hourly-priced entries (and any with no covering rate at all) are
        // priced per entry. A daily-priced entry is priced once per employee
        // per calendar day worked, regardless of hours or entry count that
        // day. A day split across more than one site puts the whole day's pay
        // on whichever site they logged the most time at, rather than
        // splitting one flat amount nobody agreed to split.
        //
        // WorkType only enters the price for Overtime and Travel — the two
        // tags a date can never tell you. Weekend and PublicHoliday come from
        // the calendar, which is always right. An Overtime or Travel shift on
        // a weekend or holiday is priced as Overtime/Travel, not stacked with
        // the weekend/holiday premium.
        var hourly = priced
            .Where(t => t.Rate is null || t.Rate.RateType == RateType.Hourly)
            .Select(t => new PricedLabourEntry(
                t.EmployeeId,
                t.ProjectId,
                t.Minutes,
                t.Rate is null
                    ? 0m
                    : (t.WorkType == WorkType.Overtime
                        ? t.Rate.OvertimeHourlyRate ?? t.Rate.HourlyRate
                    : t.WorkType == WorkType.Travel
                        ? t.Rate.TravelHourlyRate ?? t.Rate.HourlyRate
                    : t.ProjectCountryCode != null && holidays.Contains((t.ProjectCountryCode, t.Day))
                        ? t.Rate.HolidayHourlyRate ?? t.Rate.HourlyRate
                    : t.Day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                        ? t.Rate.WeekendHourlyRate ?? t.Rate.HourlyRate
                    : t.Rate.HourlyRate) is { } rate
                        ? rate * t.Minutes / 60m
                        : 0m,
                t.Rate is null ? t.Minutes : 0));

        var daily = priced
            .Where(t => t.Rate is not null && t.Rate.RateType == RateType.Daily)
            .GroupBy(t => new { t.EmployeeId, t.Day })
            .Select(g => new PricedLabourEntry(
                g.Key.EmployeeId,
                g.OrderByDescending(x => x.Minutes).First().ProjectId,
                g.Sum(x => x.Minutes),
                g.First().Rate!.DailyRate ?? 0m,
                0));

        return hourly.Concat(daily).ToList();
    }
}
