using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Common;

/// <summary>
/// What a foreman may see: the sites they are posted to, and the people posted
/// alongside them.
/// </summary>
/// <remarks>
/// A foreman runs a site, not the company. Without this they could read every
/// project and every employee, which is what the office roles are for; the
/// live location, today's hours and the weekly report were already narrowed to
/// the foreman's own sites, so the directory was the one place the boundary
/// did not hold. Every other role is unrestricted here, as before.
/// </remarks>
public static class ForemanScope
{
    /// <summary>
    /// The projects the caller is currently posted to, or null when the caller
    /// is not a foreman and so is not restricted at all.
    /// </summary>
    /// <remarks>
    /// "Currently" means the posting has not ended. Removing someone closes a
    /// started posting with today's date, so a posting whose end date is today
    /// has ended (the same rule the site roster uses). A posting that starts next
    /// week already gives the foreman the site, since the office sets it up
    /// before the first day and a foreman who cannot see the site they are
    /// about to run cannot prepare for it.
    /// </remarks>
    public static async Task<IReadOnlyList<Guid>?> OwnProjectIdsAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (currentUser.Role is not UserRole.Foreman)
        {
            return null;
        }

        if (currentUser.EmployeeId is not { } employeeId)
        {
            // A foreman account with nobody behind it is posted nowhere.
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
