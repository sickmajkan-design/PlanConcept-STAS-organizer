using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports;

/// <summary>Who hears about a weekly report, and what the notice carries to the screen it opens.</summary>
public static class WeeklyReportNotices
{
    public static Dictionary<string, string> Data(WeeklySiteReport report, string projectName, string? note = null)
    {
        var data = new Dictionary<string, string>
        {
            ["reportId"] = report.Id.ToString(),
            ["projectId"] = report.ProjectId.ToString(),
            ["projectName"] = projectName,
            ["isoYear"] = report.IsoYear.ToString(),
            ["isoWeek"] = report.IsoWeek.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(note))
        {
            data["note"] = note;
        }

        return data;
    }

    /// <summary>The foreman who sent the report: it was approved, or it has to be sent again.</summary>
    public static async Task ToSubmitterAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        WeeklySiteReport report,
        NotificationType type,
        string title,
        string body,
        string? note,
        CancellationToken cancellationToken)
    {
        var userIds = await context.Users
            .Where(u => u.IsActive && u.EmployeeId == report.SubmittedByEmployeeId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        await notifications.NotifyUsersAsync(
            userIds, type, title, body, Data(report, report.Project.Name, note), cancellationToken: cancellationToken);
    }

    /// <summary>The office: a report was sent and waits to be looked at.</summary>
    public static async Task ToOfficeAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        WeeklySiteReport report,
        string projectName,
        string submittedBy,
        Guid? exceptUserId,
        CancellationToken cancellationToken)
    {
        var recipients = await context.Users
            .Where(u => u.IsActive
                && u.Id != exceptUserId
                && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            return;
        }

        var data = Data(report, projectName);
        data["submittedBy"] = submittedBy;

        await notifications.NotifyUsersAsync(
            recipients,
            NotificationType.WeeklyReportSubmitted,
            "Weekly report waiting for review",
            $"{projectName} — KW{report.IsoWeek:D2}/{report.IsoYear}, {submittedBy}",
            data,
            cancellationToken: cancellationToken);
    }
}
