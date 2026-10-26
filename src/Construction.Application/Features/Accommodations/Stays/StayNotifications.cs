using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Stays;

internal static class StayNotifications
{
    /// <summary>Tells the person where they now live; a person with no account has nobody to tell.</summary>
    public static async Task NotifyAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        AccommodationStay stay,
        CancellationToken cancellationToken)
    {
        var userIds = await context.Users
            .Where(u => u.IsActive && u.EmployeeId == stay.EmployeeId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        var place = await context.Accommodations
            .AsNoTracking()
            .Where(a => a.Id == stay.AccommodationId)
            .Select(a => new { a.Name, a.Address })
            .FirstAsync(cancellationToken);

        var name = string.IsNullOrWhiteSpace(place.Name) ? place.Address : place.Name;

        await notifications.NotifyUsersAsync(
            userIds,
            NotificationType.AccommodationAssigned,
            "New accommodation",
            $"You have been housed at {name} from {stay.StartDate:dd.MM.yyyy}.",
            new Dictionary<string, string>
            {
                ["accommodationId"] = stay.AccommodationId.ToString(),
                ["accommodationName"] = name,
                ["startDate"] = stay.StartDate.ToString("yyyy-MM-dd")
            },
            cancellationToken: cancellationToken);
    }
}
