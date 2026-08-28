using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.AutoCloseStaleShifts;

/// <summary>
/// Closes shifts nobody clocked out of once the calendar day they started on
/// has passed.
/// </summary>
/// <remarks>
/// Before this existed, a forgotten clock-out left an employee stuck: the
/// open shift blocked a new clock-in, and past
/// <see cref="TimeEntries.TimeEntryRules.MaxShiftDuration"/> the same shift
/// refused to be clocked out of either — only a supervisor editing the row by
/// hand could unblock them. This sweep closes it first, so that situation no
/// longer happens; the employee still has to acknowledge it happened before
/// they can clock in again, via the notification this raises.
/// </remarks>
public record AutoCloseStaleShiftsCommand : IRequest<int>;

public class AutoCloseStaleShiftsCommandHandler
    : IRequestHandler<AutoCloseStaleShiftsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AutoCloseStaleShiftsCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(
        AutoCloseStaleShiftsCommand request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        // Explicit UTC kind: PostgreSQL's timestamptz columns refuse a
        // DateTime whose Kind is Unspecified, which is exactly what
        // DateOnly.ToDateTime's two-argument overload leaves it as.
        var startOfToday = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Tracked, not AsNoTracking: each row needs its own computed EndedAt,
        // and the count of these on any given day is small — someone who
        // forgot to clock out, not a bulk export.
        var stale = await _context.TimeEntries
            .Where(t => t.EndedAt == null)
            .Where(t => t.StartedAt < startOfToday)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return 0;
        }

        foreach (var entry in stale)
        {
            var startOfNextDay = entry.StartedAt.Date.AddDays(1);
            var standardEnd = entry.StartedAt.Add(TimeEntryRules.StandardShiftDuration);

            entry.EndedAt = standardEnd < startOfNextDay ? standardEnd : startOfNextDay;
            entry.Status = TimeEntryStatus.Submitted;
            entry.AutoClosed = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var entry in stale)
        {
            // No account linked is ordinary, not an error — the same
            // reasoning as WorkItemNotifier.NotifyAssignedAsync.
            var userId = await _context.Users
                .Where(u => u.EmployeeId == entry.EmployeeId && u.IsActive)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (userId is null)
            {
                continue;
            }

            await _notifications.NotifyUserAsync(
                userId.Value,
                NotificationType.ShiftAutoClosed,
                "Shift closed automatically",
                $"You did not clock out on {entry.StartedAt:dd.MM.yyyy}, so the shift was " +
                "closed automatically and is waiting for review.",
                new Dictionary<string, string> { ["timeEntryId"] = entry.Id.ToString() },
                requiresAcknowledgment: true,
                cancellationToken: cancellationToken);
        }

        return stale.Count;
    }
}
