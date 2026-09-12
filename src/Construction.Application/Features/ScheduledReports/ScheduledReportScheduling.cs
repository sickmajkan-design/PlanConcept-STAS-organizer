using Construction.Domain.Enums;

namespace Construction.Application.Features.ScheduledReports;

/// <summary>
/// Pure date arithmetic for scheduled reports: when a subscription next
/// becomes due, and what period its next report covers. Kept separate from
/// the command that uses it so both can be exercised without a database.
/// </summary>
public static class ScheduledReportScheduling
{
    /// <summary>
    /// The next occurrence of the subscription's day, always strictly after
    /// <paramref name="fromUtc"/> — creating a subscription today, on today's
    /// weekday, schedules it for next week rather than firing immediately.
    /// Midnight UTC on that day: the sweep runs roughly daily regardless of
    /// wall-clock time, so a fixed hour would not make delivery any more
    /// precise, only harder to reason about.
    /// </summary>
    public static DateTime ComputeNextRun(
        ScheduledReportCadence cadence,
        DayOfWeek? dayOfWeek,
        int? dayOfMonth,
        DateTime fromUtc)
    {
        var today = DateOnly.FromDateTime(fromUtc);

        if (cadence == ScheduledReportCadence.Weekly)
        {
            var target = dayOfWeek ?? DayOfWeek.Monday;
            var daysUntil = ((int)target - (int)today.DayOfWeek + 7) % 7;
            if (daysUntil == 0) daysUntil = 7;

            return today.AddDays(daysUntil).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        // Capped at 28 (see the entity doc comment) so this always resolves.
        var day = Math.Clamp(dayOfMonth ?? 1, 1, 28);
        var candidate = new DateOnly(today.Year, today.Month, day);

        if (candidate <= today)
        {
            candidate = candidate.AddMonths(1);
        }

        return candidate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    /// <summary>
    /// The period a report covers: the 7 days before today for a weekly
    /// subscription, the whole of the previous calendar month for a monthly
    /// one — "last week's timesheet" and "last month's fuel" regardless of
    /// which weekday or day-of-month the subscription happens to fire on.
    /// </summary>
    public static (DateOnly From, DateOnly To) ComputePeriod(
        ScheduledReportCadence cadence, DateOnly today)
    {
        if (cadence == ScheduledReportCadence.Weekly)
        {
            var to = today.AddDays(-1);
            return (to.AddDays(-6), to);
        }

        var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
        var lastOfPreviousMonth = firstOfThisMonth.AddDays(-1);

        return (new DateOnly(lastOfPreviousMonth.Year, lastOfPreviousMonth.Month, 1), lastOfPreviousMonth);
    }
}
