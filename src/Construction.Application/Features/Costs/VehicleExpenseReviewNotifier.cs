using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs;

/// <summary>
/// Tells the people who can review vehicle costs that one is waiting.
/// </summary>
/// <remarks>
/// The same audience as <see cref="CostRules.CanReviewSpending"/>, minus whoever
/// caused the notification: nobody needs a bell for what they just did
/// themselves. Every recorded cost starts Pending, so without this the
/// reviewers only find out by opening the page.
/// </remarks>
public static class VehicleExpenseReviewNotifier
{
    public static async Task NotifyAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        Guid? actingUserId,
        int count,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return;
        }

        var recipientIds = await context.Users
            .Where(u => u.IsActive &&
                        u.Id != actingUserId &&
                        (u.Role == UserRole.SuperAdmin ||
                         u.Role == UserRole.Admin ||
                         u.Role == UserRole.ProjectManager))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await notifications.NotifyUsersAsync(
            recipientIds,
            NotificationType.VehicleExpenseSubmitted,
            title,
            body,
            data,
            cancellationToken: cancellationToken);
    }
}
