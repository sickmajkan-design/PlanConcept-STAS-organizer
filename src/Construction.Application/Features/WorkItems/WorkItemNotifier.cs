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
            BuildAssignedData(item),
            item.RequiresAcknowledgment,
            cancellationToken);
    }

    /// <summary>
    /// Tells the site's foremen and project managers about a defect nobody
    /// has picked up yet — the ordinary shape a field report takes, since the
    /// worker who found the crack has no one specific to assign it to.
    /// Does nothing once <see cref="NotifyAssignedAsync"/> would already have
    /// told someone, or when the report carries no site to scope "the site's
    /// foremen" against.
    /// </summary>
    public static async Task NotifyUnassignedDefectAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        WorkItem item,
        CancellationToken cancellationToken)
    {
        if (item.Kind != WorkItemKind.Defect ||
            item.AssignedEmployeeId is not null ||
            item.ProjectId is not { } projectId)
        {
            return;
        }

        var reporterName = await context.Users
            .Where(u => u.Id == item.CreatedByUserId)
            .Select(u => u.Employee != null
                ? u.Employee.FirstName + " " + u.Employee.LastName
                : u.Email)
            .FirstOrDefaultAsync(cancellationToken) ?? "Someone";

        var recipientIds = await context.Users
            .Where(u => u.IsActive &&
                        u.Id != item.CreatedByUserId &&
                        (u.Role == UserRole.Foreman || u.Role == UserRole.ProjectManager) &&
                        u.EmployeeId != null &&
                        context.EmployeeProjects.Any(ep =>
                            ep.ProjectId == projectId && ep.EmployeeId == u.EmployeeId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await notifications.NotifyUsersAsync(
            recipientIds,
            NotificationType.DefectReported,
            "New defect reported",
            $"{reporterName} reported: {item.Title}",
            new Dictionary<string, string>
            {
                ["workItemId"] = item.Id.ToString(),
                ["projectId"] = projectId.ToString(),
                ["reporterName"] = reporterName,
                ["title"] = item.Title
            },
            cancellationToken: cancellationToken);
    }

    // The `data` dict is what a client renders its own localized sentence
    // from (see the mobile `resolveNotificationText`) — the title/due date
    // above are only the English fallback for desktop and any untemplated
    // client, so both must carry the same facts.
    private static Dictionary<string, string> BuildAssignedData(WorkItem item)
    {
        var data = new Dictionary<string, string>
        {
            ["workItemId"] = item.Id.ToString(),
            ["kind"] = item.Kind.ToString(),
            ["title"] = item.Title
        };

        if (item.DueDate is { } due)
        {
            data["dueDate"] = due.ToString("yyyy-MM-dd");
        }

        return data;
    }
}
