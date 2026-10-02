using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Branches;

/// <summary>
/// The one rule for "belongs to this business unit", used by every list and report that is
/// narrowed to a unit, so they cannot disagree. A record that names a unit itself belongs to
/// that unit; one that does not follows its project (a vehicle or tool, the project it is assigned
/// to; an expense or pay entry, the project it is booked to). Every method returns its input
/// untouched when no unit is asked for.
/// </summary>
public static class BranchScope
{
    public static IQueryable<Project> InBranch(this IQueryable<Project> query, Guid? branchId) =>
        branchId is { } id ? query.Where(p => p.BranchId == id) : query;

    public static IQueryable<Vehicle> InBranch(this IQueryable<Vehicle> query, Guid? branchId) =>
        branchId is { } id
            ? query.Where(v => v.BranchId == id
                || (v.BranchId == null && v.AssignedProject != null && v.AssignedProject.BranchId == id))
            : query;

    public static IQueryable<Tool> InBranch(this IQueryable<Tool> query, Guid? branchId) =>
        branchId is { } id
            ? query.Where(t => t.BranchId == id
                || (t.BranchId == null && t.AssignedProject != null && t.AssignedProject.BranchId == id))
            : query;

    public static IQueryable<GeneralExpense> InBranch(this IQueryable<GeneralExpense> query, Guid? branchId) =>
        branchId is { } id
            ? query.Where(e => e.BranchId == id
                || (e.BranchId == null && e.Project != null && e.Project.BranchId == id))
            : query;

    public static IQueryable<FinanceEntry> InBranch(this IQueryable<FinanceEntry> query, Guid? branchId) =>
        branchId is { } id
            ? query.Where(e => e.BranchId == id
                || (e.BranchId == null && e.Project != null && e.Project.BranchId == id))
            : query;

    /// <summary>Hours of one unit, counted by the site they were worked on or by the unit that employed the person.</summary>
    public static IQueryable<TimeEntry> InBranch(this IQueryable<TimeEntry> query, Guid? branchId, BranchBasis basis)
    {
        if (branchId is not { } id)
        {
            return query;
        }

        return basis == BranchBasis.Employer
            ? query.Where(t => t.Employee.BranchPeriods.Any(p => p.BranchId == id
                && p.StartDate <= DateOnly.FromDateTime(t.StartedAt)
                && (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(t.StartedAt))))
            : query.Where(t => t.Project != null && t.Project.BranchId == id);
    }

    /// <summary>Manual pay of one unit, counted by where it was booked or by the unit that employed the person that day.</summary>
    public static IQueryable<FinanceEntry> InBranch(this IQueryable<FinanceEntry> query, Guid? branchId, BranchBasis basis)
    {
        if (branchId is not { } id)
        {
            return query;
        }

        return basis == BranchBasis.Employer
            ? query.Where(e => e.Employee.BranchPeriods.Any(p => p.BranchId == id
                && p.StartDate <= e.OccurredOn
                && (p.EndDate == null || p.EndDate >= e.OccurredOn)))
            : query.InBranch(branchId);
    }

    public static IQueryable<CompanyRevenue> InBranch(this IQueryable<CompanyRevenue> query, Guid? branchId) =>
        branchId is { } id ? query.Where(r => r.BranchId == id) : query;

    public static IQueryable<Accommodation> InBranch(this IQueryable<Accommodation> query, Guid? branchId) =>
        branchId is { } id ? query.Where(a => a.BranchId == id) : query;

    public static IQueryable<MaterialMovement> InBranch(this IQueryable<MaterialMovement> query, Guid? branchId) =>
        branchId is { } id ? query.Where(m => m.Project != null && m.Project.BranchId == id) : query;

    public static IQueryable<ProjectRevenue> InBranch(this IQueryable<ProjectRevenue> query, Guid? branchId) =>
        branchId is { } id ? query.Where(r => r.Project.BranchId == id) : query;

    /// <summary>The ids of the vehicles of a unit, or null when no unit is asked for.</summary>
    public static async Task<HashSet<Guid>?> VehicleIdsAsync(
        IApplicationDbContext context, Guid? branchId, CancellationToken cancellationToken) =>
        branchId is null
            ? null
            : (await context.Vehicles.AsNoTracking().InBranch(branchId).Select(v => v.Id)
                .ToListAsync(cancellationToken)).ToHashSet();

    /// <summary>The ids of the tools of a unit, or null when no unit is asked for.</summary>
    public static async Task<HashSet<Guid>?> ToolIdsAsync(
        IApplicationDbContext context, Guid? branchId, CancellationToken cancellationToken) =>
        branchId is null
            ? null
            : (await context.Tools.AsNoTracking().InBranch(branchId).Select(t => t.Id)
                .ToListAsync(cancellationToken)).ToHashSet();
}
