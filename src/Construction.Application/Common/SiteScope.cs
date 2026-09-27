using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Common;

/// <summary>
/// What a site-level role may see: the sites they are posted to, and the people, work and
/// equipment on those sites — never the whole company.
/// </summary>
/// <remarks>
/// A foreman runs a site, not the company, and the customer's own instruction narrowed a
/// project manager the same way: they see the project they are posted to, its fleet and its
/// roster, "and the like" — not every site. Both roles are posted to a project exactly the way
/// a worker is (through <c>EmployeeProject</c>), so one rule covers both. Every other role is
/// unrestricted here, as before.
/// </remarks>
public static class SiteScope
{
    private static readonly UserRole[] ScopedRoles = [UserRole.ProjectManager, UserRole.Foreman];

    /// <summary>
    /// The projects the caller is currently posted to, or null when the caller holds an
    /// unrestricted role and so is not scoped at all.
    /// </summary>
    /// <remarks>
    /// "Currently" means the posting has not ended. Removing someone closes a
    /// started posting with today's date, so a posting whose end date is today
    /// has ended (the same rule the site roster uses). A posting that starts next
    /// week already gives the account the site, since the office sets it up
    /// before the first day and someone who cannot see the site they are
    /// about to run cannot prepare for it.
    /// </remarks>
    public static async Task<IReadOnlyList<Guid>?> OwnProjectIdsAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !ScopedRoles.Contains(role))
        {
            return null;
        }

        if (currentUser.EmployeeId is not { } employeeId)
        {
            // An account with nobody behind it is posted nowhere.
            return Array.Empty<Guid>();
        }

        return await context.EmployeeProjects
            .Where(ep => ep.EmployeeId == employeeId
                && (ep.EndDate == null || ep.EndDate > today))
            .Select(ep => ep.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
