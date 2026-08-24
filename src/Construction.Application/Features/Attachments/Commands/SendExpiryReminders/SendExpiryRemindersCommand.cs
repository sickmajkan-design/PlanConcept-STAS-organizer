using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Commands.SendExpiryReminders;

/// <summary>
/// Tells the office about documents that are about to lapse.
/// </summary>
/// <remarks>
/// Written as a command rather than living inside the hosted service so the
/// rule can be run on demand and tested without a scheduler.
/// </remarks>
public record SendExpiryRemindersCommand : IRequest<int>
{
    /// <summary>
    /// How much warning to give.
    /// </summary>
    /// <remarks>
    /// Thirty days is roughly how long it takes to book an occupational
    /// medical and get the certificate back, which is the slowest of the
    /// documents this tracks.
    /// </remarks>
    public int WithinDays { get; init; } = 30;
}

public class SendExpiryRemindersCommandHandler
    : IRequestHandler<SendExpiryRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendExpiryRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(
        SendExpiryRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var cutoff = DateOnly.FromDateTime(now).AddDays(request.WithinDays);

        var due = await _context.Attachments
            .Where(a => a.ExpiresAt != null
                && a.ExpiresAt <= cutoff
                && a.ExpiryReminderSentAt == null)
            .OrderBy(a => a.ExpiresAt)
            .Select(a => new
            {
                a.Id,
                a.FileName,
                a.Category,
                a.ExpiresAt,
                OwnerName = a.Employee != null
                    ? a.Employee.FirstName + " " + a.Employee.LastName
                    : a.Project != null ? a.Project.Name
                    : a.Vehicle != null ? a.Vehicle.Brand + " " + a.Vehicle.Model
                    : a.Tool != null ? a.Tool.Name
                    : null
            })
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return 0;
        }

        // Everyone who can act on it. Expiry is an office problem: the worker
        // whose certificate lapsed cannot renew it themselves.
        var recipients = await _context.Users
            .Where(u => u.IsActive
                && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            // Nobody to tell. Leaving the marks unset means the reminder still
            // goes out once an administrator exists, rather than being lost.
            return 0;
        }

        var sent = 0;

        foreach (var document in due)
        {
            // Claim the row before notifying. The update is conditional on the
            // mark still being unset, so if a second replica is running the
            // same sweep only one of them gets a row back — and nobody is told
            // twice. Doing it after the notification would leave the same row
            // claimable for the length of the push.
            var claimed = await _context.Attachments
                .Where(a => a.Id == document.Id && a.ExpiryReminderSentAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(a => a.ExpiryReminderSentAt, now),
                    cancellationToken);

            if (claimed == 0)
            {
                continue;
            }

            var expired = document.ExpiresAt < DateOnly.FromDateTime(now);

            await _notifications.NotifyUsersAsync(
                recipients,
                NotificationType.DocumentExpiring,
                expired ? "Document has expired" : "Document expiring soon",
                $"{document.FileName}" +
                (document.OwnerName is null ? "" : $" — {document.OwnerName}") +
                $" ({document.ExpiresAt:dd.MM.yyyy})",
                new Dictionary<string, string>
                {
                    ["attachmentId"] = document.Id.ToString(),
                    ["category"] = document.Category.ToString()
                },
                cancellationToken: cancellationToken);

            sent++;
        }

        return sent;
    }
}
