using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems.Commands.SendDueReminders;

/// <summary>
/// Reminds whoever the work is assigned to that a deadline is close.
/// </summary>
/// <remarks>
/// Same shape as the document-expiry sweep, and for the same reason: the row
/// is claimed with a conditional update before anything is sent, so a second
/// replica finds nothing left to claim rather than sending it twice.
/// </remarks>
public record SendDueRemindersCommand : IRequest<int>
{
    /// <summary>
    /// How much warning to give. Two days is enough to reorder a day's work
    /// around it without producing a reminder for everything on the board.
    /// </summary>
    public int WithinDays { get; init; } = 2;
}

public class SendDueRemindersCommandHandler
    : IRequestHandler<SendDueRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendDueRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(
        SendDueRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var cutoff = today.AddDays(request.WithinDays);

        var due = await _context.WorkItems
            .Where(w => w.DueDate != null
                && w.DueDate <= cutoff
                && w.DueReminderSentAt == null
                && w.Status != WorkItemStatus.Closed
                && w.Status != WorkItemStatus.Cancelled
                // Nobody to remind. Left unclaimed so the reminder still goes
                // out once it is assigned, rather than being spent on nobody.
                && w.AssignedEmployeeId != null)
            .Select(w => new
            {
                w.Id,
                w.Title,
                w.Kind,
                w.DueDate,
                w.AssignedEmployeeId
            })
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var item in due)
        {
            var userId = await _context.Users
                .Where(u => u.EmployeeId == item.AssignedEmployeeId && u.IsActive)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (userId is null)
            {
                continue;
            }

            var claimed = await _context.WorkItems
                .Where(w => w.Id == item.Id && w.DueReminderSentAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(w => w.DueReminderSentAt, now),
                    cancellationToken);

            if (claimed == 0)
            {
                continue;
            }

            var overdue = item.DueDate < today;

            await _notifications.NotifyUserAsync(
                userId.Value,
                NotificationType.WorkItemDue,
                overdue ? "Overdue" : "Due soon",
                $"{item.Title} ({item.DueDate:dd.MM.yyyy})",
                new Dictionary<string, string>
                {
                    ["workItemId"] = item.Id.ToString(),
                    ["kind"] = item.Kind.ToString()
                },
                cancellationToken: cancellationToken);

            sent++;
        }

        return sent;
    }
}
