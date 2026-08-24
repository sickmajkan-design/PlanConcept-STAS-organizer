using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems;

/// <summary>
/// Tells whoever the work landed on.
/// </summary>
/// <remarks>
/// Shared by creating and assigning, because both end the same way — someone
/// now has work they did not have a moment ago — and a notification sent from
/// one path but not the other is worse than none: people stop trusting it.
/// </remarks>
public static class WorkItemNotifier
{
    public static async Task NotifyAssignedAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        WorkItem item,
        CancellationToken cancellationToken)
    {
        if (item.AssignedEmployeeId is not { } employeeId)
        {
            return;
        }

        // An employee without an account has nowhere to receive this. That is
        // ordinary — most site staff on a first rollout have no login yet —
        // so it is not an error.
        var userId = await context.Users
            .Where(u => u.EmployeeId == employeeId && u.IsActive)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return;
        }

        await notifications.NotifyUserAsync(
            userId.Value,
            item.Kind == WorkItemKind.Defect
                ? NotificationType.DefectAssigned
                : NotificationType.TaskAssigned,
            item.Kind == WorkItemKind.Defect ? "Defect assigned to you" : "Task assigned to you",
            item.DueDate is { } due
                ? $"{item.Title} — due {due:dd.MM.yyyy}"
                : item.Title,
            new Dictionary<string, string>
            {
                ["workItemId"] = item.Id.ToString(),
                ["kind"] = item.Kind.ToString()
            },
            item.RequiresAcknowledgment,
            cancellationToken);
    }
}
