using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Exports.Queries;

/// <summary>
/// A spreadsheet holds exactly what the screen it mirrors would show. A Foreman or Project Manager sees the sites they are
/// posted to (<see cref="SiteScope"/>) on the lists, so the same narrowing applies to the file; without it the export was a
/// way round the lists. Every other role gets <c>own == null</c> and nothing changes for them.
/// </summary>
internal static class ExportSiteScope
{
    public static Task<IReadOnlyList<Guid>?> OwnAsync(
        IApplicationDbContext context, ICurrentUserService user, IDateTimeProvider clock, CancellationToken ct) =>
        SiteScope.OwnProjectIdsAsync(context, user, clock.Today, ct);

    public static IQueryable<Employee> Narrow(
        this IQueryable<Employee> query, IReadOnlyList<Guid>? own, Guid? self, DateOnly today) =>
        own is null
            ? query
            : query.Where(e => e.Id == self
                || e.ProjectAssignments.Any(pa => own.Contains(pa.ProjectId) && (pa.EndDate == null || pa.EndDate > today)));

    /// <summary>Absences of the people on the caller's sites (and the caller's own), the same crew the schedule board shows.</summary>
    public static IQueryable<Absence> Narrow(
        this IQueryable<Absence> query, IReadOnlyList<Guid>? own, Guid? self, DateOnly today) =>
        own is null
            ? query
            : query.Where(a => a.EmployeeId == self
                || a.Employee.ProjectAssignments.Any(pa => own.Contains(pa.ProjectId) && (pa.EndDate == null || pa.EndDate > today)));

    public static IQueryable<Project> Narrow(this IQueryable<Project> query, IReadOnlyList<Guid>? own) =>
        own is null ? query : query.Where(p => own.Contains(p.Id));

    public static IQueryable<Vehicle> Narrow(this IQueryable<Vehicle> query, IReadOnlyList<Guid>? own, Guid? self) =>
        own is null
            ? query
            : query.Where(v => (v.AssignedProjectId != null && own.Contains(v.AssignedProjectId.Value))
                || (self != null && v.AssignedEmployeeId == self));

    public static IQueryable<Tool> Narrow(this IQueryable<Tool> query, IReadOnlyList<Guid>? own, Guid? self) =>
        own is null
            ? query
            : query.Where(t => (t.AssignedProjectId != null && own.Contains(t.AssignedProjectId.Value))
                || (self != null && t.AssignedEmployeeId == self));

    public static IQueryable<TimeEntry> Narrow(this IQueryable<TimeEntry> query, IReadOnlyList<Guid>? own, Guid? self) =>
        own is null
            ? query
            : query.Where(t => (t.ProjectId != null && own.Contains(t.ProjectId.Value)) || (self != null && t.EmployeeId == self));
}
