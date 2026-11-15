using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments;

/// <summary>
/// A Foreman or Project Manager reads the documents of the sites they are posted to, not of every site
/// (the same rule as <see cref="SiteScope"/>, which already narrows the records themselves).
/// </summary>
public static class AttachmentSiteScope
{
    /// <summary>The caller's own projects, or null when the caller's role is not site-scoped.</summary>
    public static Task<IReadOnlyList<Guid>?> OwnProjectsAsync(
        IApplicationDbContext context, ICurrentUserService user, IDateTimeProvider clock, CancellationToken ct) =>
        SiteScope.OwnProjectIdsAsync(context, user, clock.Today, ct);

    /// <summary>
    /// Whether a document on this project, or on this work item, is within the caller's sites. Documents on anything else are
    /// not decided here. A work item with no project is nobody's site.
    /// </summary>
    public static bool Allows(
        IReadOnlyList<Guid>? own, Guid? callerEmployeeId, Guid? projectId, Guid? workItemId, Guid? workItemProjectId, Guid? workItemAssignee)
    {
        if (own is null)
        {
            return true;
        }

        if (projectId is { } project)
        {
            return own.Contains(project);
        }

        if (workItemId is not null && workItemProjectId is { } itemProject)
        {
            return own.Contains(itemProject) || (callerEmployeeId is not null && workItemAssignee == callerEmployeeId);
        }

        return true;
    }

    /// <summary>The same decision for a project or work item named by id, as the list and count queries have it.</summary>
    public static async Task<bool> AllowsOwnerAsync(
        IApplicationDbContext context, IReadOnlyList<Guid>? own, Guid? callerEmployeeId,
        AttachmentOwnerType ownerType, Guid ownerId, CancellationToken ct)
    {
        if (own is null)
        {
            return true;
        }

        if (ownerType == AttachmentOwnerType.Project)
        {
            return own.Contains(ownerId);
        }

        if (ownerType == AttachmentOwnerType.WorkItem)
        {
            var item = await context.WorkItems.AsNoTracking()
                .Where(w => w.Id == ownerId)
                .Select(w => new { w.ProjectId, w.AssignedEmployeeId })
                .FirstOrDefaultAsync(ct);

            return item is null || Allows(own, callerEmployeeId, null, ownerId, item.ProjectId, item.AssignedEmployeeId);
        }

        return true;
    }
}
