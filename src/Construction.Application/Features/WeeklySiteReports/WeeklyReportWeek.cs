using System.Globalization;
using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports;

/// <summary>
/// The week a weekly site report is due for, when it is due, and which sites are still missing one. The
/// reminder and the office's overview both ask the same question, so they ask it here.
/// </summary>
public static class WeeklyReportWeek
{
    /// <summary>The report is due by this time on the Monday after the week.</summary>
    public static readonly TimeOnly DeadlineTime = new(12, 0);

    /// <summary>The reminder to the foreman goes out on the Tuesday, from this time on.</summary>
    public static readonly TimeOnly ReminderTime = new(7, 0);

    public readonly record struct Week(int IsoYear, int IsoWeek, DateOnly Start, DateOnly End, DateTime DeadlineUtc);

    /// <summary>One person posted to a site that has no report for the week.</summary>
    public readonly record struct MissingAssignment(Guid ProjectId, string ProjectName, Guid EmployeeId);

    /// <summary>The last week that is over: the Monday-to-Sunday before the most recent Monday.</summary>
    public static Week Last(DateTime utcNow)
    {
        var today = CompanyTime.LocalDate(utcNow);
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var end = monday.AddDays(-1);
        var start = end.AddDays(-6);
        var asDate = end.ToDateTime(TimeOnly.MinValue);

        return new Week(
            ISOWeek.GetYear(asDate),
            ISOWeek.GetWeekOfYear(asDate),
            start,
            end,
            CompanyTime.StartOfDayUtc(monday).Add(DeadlineTime.ToTimeSpan()));
    }

    public static bool IsPastDeadline(Week week, DateTime utcNow) => utcNow >= week.DeadlineUtc;

    /// <summary>
    /// Everyone posted to a site during the week, where the site has no report for it yet. A report that was sent
    /// back for correction still counts as sent: the foreman owes a new one, not a first one.
    /// </summary>
    public static async Task<IReadOnlyList<MissingAssignment>> MissingAsync(
        IApplicationDbContext context, Week week, CancellationToken cancellationToken)
    {
        var assignments = await context.EmployeeProjects
            .Where(a => a.StartDate <= week.End && (a.EndDate == null || a.EndDate >= week.Start))
            .Select(a => new { a.ProjectId, a.EmployeeId, ProjectName = a.Project.Name })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return [];
        }

        var projectIds = assignments.Select(a => a.ProjectId).Distinct().ToList();

        var reported = await context.WeeklySiteReports
            .Where(r => r.IsoYear == week.IsoYear && r.IsoWeek == week.IsoWeek && projectIds.Contains(r.ProjectId))
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var reportedSet = reported.ToHashSet();

        return assignments
            .Where(a => !reportedSet.Contains(a.ProjectId))
            .Select(a => new MissingAssignment(a.ProjectId, a.ProjectName, a.EmployeeId))
            .ToList();
    }
}
