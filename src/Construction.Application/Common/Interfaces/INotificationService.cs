using Construction.Domain.Enums;

namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Delivers a notification to users: persists an inbox row for each
/// recipient and pushes to their registered devices via FCM. Never throws —
/// notification delivery must not break the business operation it follows.
/// </summary>
public interface INotificationService
{
    Task NotifyUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        bool requiresAcknowledgment = false,
        CancellationToken cancellationToken = default);

    /// <returns>The number of recipients that were persisted.</returns>
    Task<int> NotifyUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationType type,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        bool requiresAcknowledgment = false,
        CancellationToken cancellationToken = default);
}
