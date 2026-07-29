using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IPushSender _pushSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        IPushSender pushSender,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _pushSender = pushSender;
        _logger = logger;
    }

    public Task NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
        => NotifyUsersAsync([userId], type, title, body, data, cancellationToken);

    public async Task<int> NotifyUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationType type,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
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

            foreach (var userId in userIds.Distinct())
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Body = body,
                    DataJson = dataJson
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            await PushAsync(userIds, type, title, body, payload, cancellationToken);

            return userIds.Distinct().Count();
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

    private async Task PushAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationType type,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        var tokens = await _context.DeviceTokens
            .Where(dt => userIds.Contains(dt.UserId))
            .Select(dt => dt.Token)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            return;
        }

        var pushData = new Dictionary<string, string>(data)
        {
            ["notificationType"] = type.ToString()
        };

        var result = await _pushSender.SendAsync(tokens, title, body, pushData, cancellationToken);

        if (result.InvalidTokens.Count > 0)
        {
            // Prune tokens FCM reports as permanently dead (uninstalled apps etc.).
            await _context.DeviceTokens
                .Where(dt => result.InvalidTokens.Contains(dt.Token))
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Pruned {Count} invalid device token(s) after push", result.InvalidTokens.Count);
        }
    }
}
