using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests.Commands.SendDocumentRequestReminders;

/// <summary>
/// The daily sweep over the deadlines of requests for documents: the person is told the day before and on
/// the day, and the office is told once that the deadline has passed with nothing sent.
/// </summary>
/// <remarks>
/// Only requests that still wait on the person count (open, or sent back): one that was sent and waits to
/// be checked, accepted or withdrawn has nothing left to remind anyone of. Each reminder is claimed with a
/// row first (see <see cref="DocumentRequestReminder"/>), exactly like the document-expiry sweep, so a
/// second run, or two servers, never repeat a notice. Nothing in the text says what is asked for, since a
/// notice is read on a lock screen.
/// </remarks>
public record SendDocumentRequestRemindersCommand : IRequest<int>;

public class SendDocumentRequestRemindersCommandHandler
    : IRequestHandler<SendDocumentRequestRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendDocumentRequestRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(SendDocumentRequestRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var tomorrow = today.AddDays(1);

        var waiting = await _context.DocumentRequests
            .Include(r => r.Employee)
            .Where(r => r.DueDate != null
                && r.DueDate <= tomorrow
                && (r.Status == DocumentRequestStatus.Open || r.Status == DocumentRequestStatus.Returned))
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var item in waiting)
        {
            var due = item.DueDate!.Value;
            var kind = due < today
                ? DocumentRequestReminderKind.Overdue
                : due == today
                    ? DocumentRequestReminderKind.OnDue
                    : DocumentRequestReminderKind.DayBefore;

            var claim = new DocumentRequestReminder
            {
                DocumentRequestId = item.Id,
                Kind = kind,
                DueDate = due,
                SentAt = now
            };
            _context.DocumentRequestReminders.Add(claim);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Already claimed by an earlier run (or another server): nothing to repeat.
                _context.DocumentRequestReminders.Remove(claim);
                continue;
            }

            var data = new Dictionary<string, string> { ["requestId"] = item.Id.ToString() };

            if (kind == DocumentRequestReminderKind.Overdue)
            {
                var office = await DocumentRequestRules.OfficeRecipientsAsync(_context, item, cancellationToken);

                if (office.Count == 0)
                {
                    continue;
                }

                data["employeeName"] = $"{item.Employee.FirstName} {item.Employee.LastName}".Trim();

                sent += await _notifications.NotifyUsersAsync(
                    office,
                    NotificationType.DocumentRequestOverdue,
                    "Document request is overdue",
                    "The deadline of a request for a document has passed and nothing was sent.",
                    data,
                    cancellationToken: cancellationToken);

                continue;
            }

            var people = await DocumentRequestRules.UserIdsOfEmployeeAsync(_context, item.EmployeeId, cancellationToken);

            if (people.Count == 0)
            {
                continue;
            }

            var dueToday = kind == DocumentRequestReminderKind.OnDue;
            data["when"] = dueToday ? "today" : "tomorrow";

            sent += await _notifications.NotifyUsersAsync(
                people,
                NotificationType.DocumentRequestDue,
                dueToday ? "A request is due today" : "A request is due tomorrow",
                "A request from the office is due. Open the app to see it.",
                data,
                cancellationToken: cancellationToken);
        }

        return sent;
    }
}
