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
                // A document with its own lead times is handled below instead.
                .Where(a => a.ReminderDays.Length == 0)
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
                    BuildData(document.Id, document.Category, document.FileName, document.OwnerName, document.ExpiresAt, expired),
                    cancellationToken: cancellationToken);

                sent++;
            }
        }

        sent += await SendOwnRemindersAsync(admins.Select(a => a.Id).ToList(), now, today, cancellationToken);

        return sent;
    }

    /// <summary>
    /// Documents with their own lead times (a visa at 90 and 30 days): every admin is told once
    /// the first of those dates is reached, and again for each later one. The person who added
    /// the document is named in it, since a lapsing visa is first of all theirs to chase.
    /// </summary>
    private async Task<int> SendOwnRemindersAsync(
        List<Guid> adminIds,
        DateTime now,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var documents = await _context.Attachments
            .Where(a => a.ExpiresAt != null && a.ReminderDays.Length > 0)
            .Select(a => new
            {
                a.Id,
                a.FileName,
                a.Category,
                a.ExpiresAt,
                a.ReminderDays,
                a.UploadedByUserId,
                OwnerName = a.Employee != null
                    ? a.Employee.FirstName + " " + a.Employee.LastName
                    : a.Project != null ? a.Project.Name
                    : a.Vehicle != null ? a.Vehicle.Brand + " " + a.Vehicle.Model
                    : a.Tool != null ? a.Tool.Name
                    : null
            })
            .ToListAsync(cancellationToken);

        // Only documents whose widest lead time has been reached are of interest.
        documents = documents
            .Where(d => d.ExpiresAt!.Value.AddDays(-d.ReminderDays.Max()) <= today)
            .ToList();

        if (documents.Count == 0)
        {
            return 0;
        }

        var ids = documents.Select(d => d.Id).ToList();
        var claimed = (await _context.AttachmentExpiryReminders
                .Where(r => ids.Contains(r.AttachmentId) && r.DaysBefore > 0)
                .Select(r => new { r.AttachmentId, r.UserId, r.DaysBefore })
                .ToListAsync(cancellationToken))
            .Select(r => (r.AttachmentId, r.UserId, r.DaysBefore))
            .ToHashSet();

        var sent = 0;

        foreach (var document in documents)
        {
            var reached = document.ReminderDays
                .Where(days => document.ExpiresAt!.Value.AddDays(-days) <= today)
                .ToList();

            foreach (var adminId in adminIds)
            {
                var fresh = reached
                    .Where(days => !claimed.Contains((document.Id, adminId, days)))
                    .ToList();

                if (fresh.Count == 0)
                {
                    continue;
                }

                // One message however many lead times were passed since the last sweep.
                var claims = fresh.Select(days => new AttachmentExpiryReminder
                {
                    AttachmentId = document.Id,
                    UserId = adminId,
                    SentAt = now,
                    DaysBefore = days
                }).ToList();

                _context.AttachmentExpiryReminders.AddRange(claims);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    _context.AttachmentExpiryReminders.RemoveRange(claims);
                    continue;
                }

                foreach (var days in fresh)
                {
                    claimed.Add((document.Id, adminId, days));
                }

                var expired = document.ExpiresAt < today;
                var yours = document.UploadedByUserId == adminId;
                var data = BuildData(document.Id, document.Category, document.FileName, document.OwnerName, document.ExpiresAt, expired);
                data["daysLeft"] = Math.Max(0, document.ExpiresAt!.Value.DayNumber - today.DayNumber).ToString();

                if (yours)
                {
                    data["addedByYou"] = "true";
                }

                await _notifications.NotifyUserAsync(
                    adminId,
                    NotificationType.DocumentExpiring,
                    expired ? "Document has expired" : "Document expiring soon",
                    $"{document.FileName}" +
                    (document.OwnerName is null ? "" : $" — {document.OwnerName}") +
                    $" ({document.ExpiresAt:dd.MM.yyyy})" +
                    (yours ? " — you added this document" : ""),
                    data,
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
        DateOnly? expiresAt,
        bool expired)
    {
        var data = new Dictionary<string, string>
        {
            ["attachmentId"] = attachmentId.ToString(),
            ["category"] = category.ToString(),
            ["fileName"] = fileName,
            ["expired"] = expired ? "true" : "false"
        };

        if (ownerName is not null)
        {
            data["ownerName"] = ownerName;
        }

        if (expiresAt is { } date)
        {
            data["expiresAt"] = date.ToString("yyyy-MM-dd");
        }

        return data;
    }
}
