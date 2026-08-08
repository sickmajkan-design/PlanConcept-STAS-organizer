using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Common.Security;

/// <summary>
/// Whose movements a caller may look at.
/// </summary>
/// <remarks>
/// <para>
/// The role policies say who may call the tracking endpoints; until now
/// nothing said <em>about whom</em>. A foreman running one site could open the
/// live map and watch every employee in the company, or pull a week of
/// somebody's minute-by-minute movements from a site they have nothing to do
/// with. That is not a role failure — the foreman is allowed to use the map —
/// it is the absence of a row-level rule underneath it.
/// </para>
/// <para>
/// <strong>The decision, since the audit left it open:</strong> a foreman sees
/// the crews on the projects they are themselves currently posted to, and
/// nobody else. Everyone from project manager upwards sees everyone.
/// </para>
/// <para>
/// Why the line falls there rather than one rung higher. A foreman is
/// definitionally on a site with a crew, and that crew is exactly what the
/// data already says: their own current postings. A project manager is an
/// office role that may have no postings at all, so scoping them to their
/// postings would silently show them an empty map — a rule that breaks the
/// people it applies to gets removed rather than obeyed. If project managers
/// should be scoped too, the honest version is a "manages this project"
/// relationship that does not exist in the schema yet, not a reuse of this
/// one.
/// </para>
/// <para>
/// This deliberately scopes <em>location</em> only, not the staff directory. A
/// continuous record of where somebody has been is a different kind of thing
/// from their name, position and work number, which a company shares
/// internally anyway — and narrowing the directory would stop a foreman
/// looking up the phone number of the person they need on site in ten minutes.
/// </para>
/// </remarks>
public static class CrewVisibility
{
    /// <summary>
    /// True when the caller may see every employee's movements.
    /// </summary>
    public static bool SeesEveryone(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin or UserRole.ProjectManager;

    /// <summary>
    /// Narrows a set of employees to the ones whose movements
    /// <paramref name="user"/> may see.
    /// </summary>
    /// <remarks>
    /// Returns an <see cref="IQueryable{T}"/> rather than a list, so the
    /// restriction becomes part of the same SQL statement the caller was
    /// already running. Filtering after the fact would work and would still
    /// have fetched every row first, which is the wrong shape for a rule whose
    /// point is that the data does not leave the database.
    /// </remarks>
    public static IQueryable<Employee> Restrict(
        IQueryable<Employee> employees,
        IQueryable<EmployeeProject> postings,
        ICurrentUserService user,
        DateOnly today)
    {
        if (SeesEveryone(user.Role))
        {
            return employees;
        }

        if (user.EmployeeId is not { } employeeId)
        {
            // A foreman's account with no employee record behind it supervises
            // nobody, so it sees nobody. Empty rather than everybody: the
            // failure mode of guessing the other way is the exact leak this
            // exists to close.
            return employees.Where(_ => false);
        }

        var supervised = postings
            .Where(p => p.EmployeeId == employeeId
                && p.StartDate <= today
                && (p.EndDate == null || p.EndDate >= today))
            .Select(p => p.ProjectId);

        return employees.Where(e =>
            // Always themselves. A foreman between postings can still see
            // their own track, which is the one record they are entitled to
            // under any reading.
            e.Id == employeeId
            || e.ProjectAssignments.Any(pa =>
                supervised.Contains(pa.ProjectId)
                && pa.StartDate <= today
                && (pa.EndDate == null || pa.EndDate >= today)));
    }

    /// <summary>
    /// Whether one employee's movements are visible to
    /// <paramref name="user"/>.
    /// </summary>
    /// <remarks>
    /// Asks the same question <see cref="Restrict"/> answers, so the two
    /// cannot drift: a single-employee lookup is the restricted set narrowed
    /// to one id.
    /// </remarks>
    public static Task<bool> CanSeeAsync(
        IQueryable<Employee> employees,
        IQueryable<EmployeeProject> postings,
        ICurrentUserService user,
        Guid employeeId,
        DateOnly today,
        CancellationToken cancellationToken) =>
        Restrict(employees, postings, user, today)
            .AnyAsync(e => e.Id == employeeId, cancellationToken);
}
