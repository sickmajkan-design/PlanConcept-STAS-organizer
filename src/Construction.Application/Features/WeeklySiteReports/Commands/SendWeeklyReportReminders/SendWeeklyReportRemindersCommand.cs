using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.SendWeeklyReportReminders;

/// <summary>
/// Tuesday morning's check: which sites had nobody send last ISO week's report by Monday noon, and who to nudge
/// about it — the foreman posted to that site.
/// </summary>
/// <remarks>
/// A no-op on Mondays and before the reminder time on Tuesdays. From then on it sends, once per site, week and
/// foreman: a sweep that missed the Tuesday morning still sends later in the week, and one that runs every hour
/// finds the claim already taken.
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
        var today = CompanyTime.LocalDate(now);

        if (today.DayOfWeek == DayOfWeek.Monday
            || (today.DayOfWeek == DayOfWeek.Tuesday
                && CompanyTime.LocalTimeOfDay(now) < WeeklyReportWeek.ReminderTime))
        {
            return 0;
        }

        var week = WeeklyReportWeek.Last(now);
        var missing = await WeeklyReportWeek.MissingAsync(_context, week, cancellationToken);

        if (missing.Count == 0)
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

        var sent = 0;

        foreach (var assignment in missing)
        {
            foreach (var userId in foremenByEmployeeId[assignment.EmployeeId])
            {
                var claim = new WeeklyReportReminder
                {
                    ProjectId = assignment.ProjectId,
                    EmployeeId = assignment.EmployeeId,
                    IsoYear = week.IsoYear,
                    IsoWeek = week.IsoWeek,
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
                    $"{assignment.ProjectName} — KW{week.IsoWeek:D2}/{week.IsoYear}",
                    new Dictionary<string, string>
                    {
                        ["projectId"] = assignment.ProjectId.ToString(),
                        ["projectName"] = assignment.ProjectName,
                        ["isoYear"] = week.IsoYear.ToString(),
                        ["isoWeek"] = week.IsoWeek.ToString(),
                    },
                    cancellationToken: cancellationToken);

                sent++;
            }
        }

        return sent;
    }
}
