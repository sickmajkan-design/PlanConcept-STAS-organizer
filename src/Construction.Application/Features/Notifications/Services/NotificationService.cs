using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Outbox;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IOutbox _outbox;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        IOutbox outbox,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _outbox = outbox;
        _logger = logger;
    }

    public Task NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        bool requiresAcknowledgment = false,
        CancellationToken cancellationToken = default)
        => NotifyUsersAsync(
            [userId], type, title, body, data, requiresAcknowledgment, cancellationToken);

    public async Task<int> NotifyUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationType type,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        bool requiresAcknowledgment = false,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return 0;
        }

        try
        {
            var payload = data ?? new Dictionary<string, string>();
            var dataJson = payload.Count > 0 ? JsonSerializer.Serialize(payload) : null;

            var recipients = userIds.Distinct().ToList();

            foreach (var userId in recipients)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Body = body,
                    DataJson = dataJson,
                    RequiresAcknowledgment = requiresAcknowledgment
                });
            }

            // The inbox row is written here and the push is queued; both
            // commit together below. Pushing inline used to put an FCM round
            // trip inside whatever operation raised the notification —
            // assigning an employee to a site waited on Google — and a
            // transient failure lost the push with no retry. The inbox row
            // stays inline because it is the record: both clients read it, and
            // a notification that exists only as a queued push has not
            // happened yet as far as anyone looking at their inbox is
            // concerned.
            _outbox.Enqueue(new PushPayload(recipients, type, title, body, payload));

            await _context.SaveChangesAsync(cancellationToken);

            return recipients.Count;
        }
        catch (Exception ex)
        {
            // Notifications are best-effort; the triggering operation already succeeded.
            _logger.LogError(ex,
                "Failed to deliver {Type} notification '{Title}' to {Count} user(s)",
                type, title, userIds.Count);
            return 0;
        }
    }
}
