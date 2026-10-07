using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees;

/// <summary>
/// Tells an employee, and the supervisors already on the site, that the employee has been put on it.
/// Shared by every way of posting someone, so a posting made from the planning screen reads the same
/// on the phone as one made from the employee's page.
/// </summary>
internal static class ProjectAssignmentNotifier
{
    public static async Task NotifyAsync(
        IApplicationDbContext context,
        INotificationService notificationService,
        Employee employee,
        Project project,
        CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["projectId"] = project.Id.ToString(),
            ["employeeId"] = employee.Id.ToString(),
            // Carried alongside the plain-English Title/Body below so a
            // client can render its own, localized version instead — the
            // point of sending someone to a site is that they know where it
            // is, and "New project assigned" alone does not say that.
            ["projectName"] = project.Name,
            ["employeeName"] = employee.FullName
        };

        if (!string.IsNullOrWhiteSpace(project.Address))
        {
            data["projectAddress"] = project.Address;
        }

        if (project.ShiftStartTime is { } shiftStartTime)
        {
            data["projectShiftStartTime"] = shiftStartTime.ToString("HH:mm");
        }

        // The assigned employee learns about their new project — where it is
        // and, if the site has one, what time the shift starts, not just its
        // name. The English fallback below is what desktop and an
        // untemplated client show as-is; the mobile app builds its own
        // localized sentence from `data` instead (see `resolveNotificationText`).
        if (employee.User is { IsActive: true } user)
        {
            var body = string.IsNullOrWhiteSpace(project.Address)
                ? $"You have been assigned to project '{project.Name}'."
                : $"You have been assigned to project '{project.Name}', at {project.Address}.";

            await notificationService.NotifyUserAsync(
                user.Id,
                NotificationType.ProjectAssigned,
                "New project assigned",
                body,
                data,
                cancellationToken: cancellationToken);
        }

        // Foremen and project managers already on the crew learn about the newcomer.
        var supervisorIds = await context.Users
            .Where(u => u.IsActive &&
                        u.EmployeeId != null &&
                        u.EmployeeId != employee.Id &&
                        (u.Role == UserRole.ProjectManager || u.Role == UserRole.Foreman) &&
                        context.EmployeeProjects.Any(ep =>
                            ep.ProjectId == project.Id && ep.EmployeeId == u.EmployeeId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await notificationService.NotifyUsersAsync(
            supervisorIds,
            NotificationType.EmployeeAssigned,
            "Employee assigned to your project",
            $"{employee.FullName} has been assigned to project '{project.Name}'.",
            data,
            cancellationToken: cancellationToken);
    }
}
