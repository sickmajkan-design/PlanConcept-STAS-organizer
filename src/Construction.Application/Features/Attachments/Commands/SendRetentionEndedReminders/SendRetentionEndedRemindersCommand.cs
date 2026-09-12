using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Commands.SendRetentionEndedReminders;

/// <summary>
/// Tells every admin about a document whose mandatory retention period has
/// just ended — informational only. Nothing is deleted here or anywhere else
/// automatically: a person decides whether the document is still needed and
/// removes it themselves through the ordinary delete action, exactly the way
/// <see cref="Features.Attachments.Commands.SendExpiryReminders.SendExpiryRemindersCommand"/>
/// only ever notifies about a lapsing certificate rather than acting on it.
/// </summary>
public record SendRetentionEndedRemindersCommand : IRequest<int>;

public class SendRetentionEndedRemindersCommandHandler
    : IRequestHandler<SendRetentionEndedRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendRetentionEndedRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(
        SendRetentionEndedRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Unlike expiry, retention has no per-admin lead time — there is
        // nothing to give advance warning of, only a date that has already
        // passed, so every admin is told on the same sweep that finds it.
        var admins = await _context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var admin in admins)
        {
            var due = await _context.Attachments
                .Where(a => a.RetainUntil != null && a.RetainUntil < today)
                // Not yet claimed for this admin specifically, same as the
                // expiry sweep's own dedup.
                .Where(a => !_context.AttachmentRetentionReminders
                    .Any(r => r.AttachmentId == a.Id && r.UserId == admin))
                .OrderBy(a => a.RetainUntil)
                .Select(a => new
                {
                    a.Id,
                    a.FileName,
                    a.Category,
                    a.RetainUntil,
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
                // The claim row itself is the dedup, same as the expiry
                // sweep — a second run hitting the unique (AttachmentId,
                // UserId) index fails the insert and skips notifying.
                var claim = new AttachmentRetentionReminder
                {
                    AttachmentId = document.Id,
                    UserId = admin,
                    SentAt = now
                };
                _context.AttachmentRetentionReminders.Add(claim);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    _context.AttachmentRetentionReminders.Remove(claim);
                    continue;
                }

                await _notifications.NotifyUserAsync(
                    admin,
                    NotificationType.DocumentRetentionEnded,
                    "Document retention period ended",
                    $"{document.FileName}" +
                    (document.OwnerName is null ? "" : $" — {document.OwnerName}") +
                    $" no longer has to be kept (was until {document.RetainUntil:dd.MM.yyyy})." +
                    " Delete it yourself if it is no longer needed.",
                    BuildData(document.Id, document.Category, document.FileName, document.OwnerName, document.RetainUntil),
                    cancellationToken: cancellationToken);

                sent++;
            }
        }

        return sent;
    }

    private static Dictionary<string, string> BuildData(
        Guid attachmentId,
        AttachmentCategory category,
        string fileName,
        string? ownerName,
        DateOnly? retainUntil)
    {
        var data = new Dictionary<string, string>
        {
            ["attachmentId"] = attachmentId.ToString(),
            ["category"] = category.ToString(),
            ["fileName"] = fileName
        };

        if (ownerName is not null)
        {
            data["ownerName"] = ownerName;
        }

        if (retainUntil is { } date)
        {
            data["retainUntil"] = date.ToString("yyyy-MM-dd");
        }

        return data;
    }
}
