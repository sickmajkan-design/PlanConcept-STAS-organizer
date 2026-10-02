using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Branches;

/// <summary>
/// Tells an employee that the business unit that employs them has changed. They find out where they
/// stand — and, for those who check payslips, which unit pays them — without having to ask.
/// </summary>
public static class EmployeeBranchNotifier
{
    public static async Task NotifyAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        IReadOnlyCollection<(Guid EmployeeId, DateOnly From)> moved,
        string? branchName,
        CancellationToken cancellationToken)
    {
        if (moved.Count == 0)
        {
            return;
        }

        var ids = moved.Select(m => m.EmployeeId).ToList();

        var users = await context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.EmployeeId != null && ids.Contains(u.EmployeeId.Value))
            .Select(u => new { u.Id, EmployeeId = u.EmployeeId!.Value })
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            var from = moved.First(m => m.EmployeeId == user.EmployeeId).From;

            var data = new Dictionary<string, string> { ["startDate"] = from.ToString("yyyy-MM-dd") };

            if (branchName is not null)
            {
                data["branchName"] = branchName;
            }

            await notifications.NotifyUserAsync(
                user.Id,
                NotificationType.EmployeeBranchChanged,
                "Business unit changed",
                branchName is null
                    ? $"From {from:dd.MM.yyyy.} you are not assigned to any business unit."
                    : $"You are employed in {branchName} from {from:dd.MM.yyyy.}.",
                data,
                cancellationToken: cancellationToken);
        }
    }
}
