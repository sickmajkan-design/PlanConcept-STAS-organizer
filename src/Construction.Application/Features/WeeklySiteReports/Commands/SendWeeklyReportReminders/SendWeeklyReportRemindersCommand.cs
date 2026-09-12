using System.Globalization;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.SendWeeklyReportReminders;

/// <summary>
/// Monday's check: which sites had nobody assigned to them submit last ISO
/// week's report yet, and who to nudge about it.
/// </summary>
/// <remarks>
/// A no-op on every other day of the week — the daily sweep this rides
/// inside of runs roughly every 24 hours regardless of weekday, and "every
/// day" is not what "every Monday" asked for.
/// </remarks>
public record SendWeeklyReportRemindersCommand : IRequest<int>;

public class SendWeeklyReportRemindersCommandHandler
    : IRequestHandler<SendWeeklyReportRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendWeeklyReportRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(SendWeeklyReportRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now);

        if (today.DayOfWeek != DayOfWeek.Monday)
        {
            return 0;
        }

        var lastWeekEnd = today.AddDays(-1);
        var lastWeekStart = lastWeekEnd.AddDays(-6);
        var isoYear = ISOWeek.GetYear(lastWeekEnd.ToDateTime(TimeOnly.MinValue));
        var isoWeek = ISOWeek.GetWeekOfYear(lastWeekEnd.ToDateTime(TimeOnly.MinValue));

        // Every site with someone posted to it at any point during that
        // week, and whether it already has a report for it.
        var sitesWithAssignments = await _context.EmployeeProjects
            .Where(a => a.StartDate <= lastWeekEnd && (a.EndDate == null || a.EndDate >= lastWeekStart))
            .Select(a => new { a.ProjectId, a.EmployeeId })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (sitesWithAssignments.Count == 0)
        {
            return 0;
        }

        var projectIds = sitesWithAssignments.Select(a => a.ProjectId).Distinct().ToList();

        var reportedProjectIds = await _context.WeeklySiteReports
            .Where(r => r.IsoYear == isoYear && r.IsoWeek == isoWeek && projectIds.Contains(r.ProjectId))
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingProjectIds = projectIds.Except(reportedProjectIds).ToHashSet();

        if (missingProjectIds.Count == 0)
        {
            return 0;
        }

        // Only foremen submit reports, so only foremen are reminded to —
        // same narrowing CreateWeeklySiteReportCommand applies the other way.
        var foremen = await _context.Users
            .Where(u => u.IsActive && u.Role == UserRole.Foreman && u.EmployeeId != null)
            .Select(u => new { u.Id, u.EmployeeId })
            .ToListAsync(cancellationToken);

        var foremenByEmployeeId = foremen
            .Where(u => u.EmployeeId is not null)
            .ToLookup(u => u.EmployeeId!.Value, u => u.Id);

        var projectNames = await _context.Projects
            .Where(p => missingProjectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var sent = 0;

        foreach (var assignment in sitesWithAssignments)
        {
            if (!missingProjectIds.Contains(assignment.ProjectId)) continue;

            foreach (var userId in foremenByEmployeeId[assignment.EmployeeId])
            {
                var claim = new WeeklyReportReminder
                {
                    ProjectId = assignment.ProjectId,
                    EmployeeId = assignment.EmployeeId,
                    IsoYear = isoYear,
                    IsoWeek = isoWeek,
                    SentAt = now,
                };
                _context.WeeklyReportReminders.Add(claim);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Already reminded — the unique index is the dedup.
                    _context.WeeklyReportReminders.Remove(claim);
                    continue;
                }

                await _notifications.NotifyUserAsync(
                    userId,
                    NotificationType.WeeklyReportDue,
                    "Weekly hours not yet submitted",
                    $"{projectNames.GetValueOrDefault(assignment.ProjectId, "")} — KW{isoWeek:D2}/{isoYear}",
                    new Dictionary<string, string>
                    {
                        ["projectId"] = assignment.ProjectId.ToString(),
                        ["projectName"] = projectNames.GetValueOrDefault(assignment.ProjectId, ""),
                        ["isoYear"] = isoYear.ToString(),
                        ["isoWeek"] = isoWeek.ToString(),
                    },
                    cancellationToken: cancellationToken);

                sent++;
            }
        }

        return sent;
    }
}
