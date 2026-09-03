using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Commands.SendExpiryReminders;

/// <summary>
/// Tells each admin about documents lapsing within the window they've
/// personally asked for.
/// </summary>
/// <remarks>
/// Written as a command rather than living inside the hosted service so the
/// rule can be run on demand and tested without a scheduler.
/// </remarks>
public record SendExpiryRemindersCommand : IRequest<int>
{
    /// <summary>
    /// What an admin gets who has never set their own preference
    /// (<see cref="User.DocumentExpiryReminderDays"/> is null). Thirty days is
    /// roughly how long it takes to book an occupational medical and get the
    /// certificate back, which is the slowest of the documents this tracks.
    /// </summary>
    public const int DefaultReminderDays = 30;
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
        var today = DateOnly.FromDateTime(now);

        // Expiry is an office problem: the worker whose certificate lapsed
        // cannot renew it themselves. Each admin gets their own lead time —
        // one might want six weeks' warning, another finds that noisy and
        // only wants the final fortnight.
        var admins = await _context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => new { u.Id, u.DocumentExpiryReminderDays })
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var admin in admins)
        {
            var days = admin.DocumentExpiryReminderDays ?? SendExpiryRemindersCommand.DefaultReminderDays;
            var cutoff = today.AddDays(days);

            var due = await _context.Attachments
                .Where(a => a.ExpiresAt != null && a.ExpiresAt <= cutoff)
                // Not yet claimed for this admin specifically — a different
                // admin with a wider window may already have been told about
                // the same document without that meaning anything for this one.
                .Where(a => !_context.AttachmentExpiryReminders
                    .Any(r => r.AttachmentId == a.Id && r.UserId == admin.Id))
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

            foreach (var document in due)
            {
                // The claim row itself is the dedup: a second sweep hitting
                // the unique (AttachmentId, UserId) index fails the insert
                // and skips notifying, rather than a separate flag checked
                // then set as two steps a concurrent run could interleave.
                var claim = new AttachmentExpiryReminder
                {
                    AttachmentId = document.Id,
                    UserId = admin.Id,
                    SentAt = now
                };
                _context.AttachmentExpiryReminders.Add(claim);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    // Someone else claimed it first. Undo the pending insert
                    // on this context — otherwise every later iteration's
                    // SaveChangesAsync keeps retrying this same failed row
                    // and none of them ever succeed.
                    _context.AttachmentExpiryReminders.Remove(claim);
                    continue;
                }

                var expired = document.ExpiresAt < today;

                await _notifications.NotifyUserAsync(
                    admin.Id,
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
        }

        return sent;
    }
}
