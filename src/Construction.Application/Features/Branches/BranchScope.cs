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
/// <remarks>
/// Asking for a unit asks for it and everything nested under it: a record belongs when the
/// unit it names has the asked-for unit anywhere in its <see cref="Branch.Path"/>
/// (see <see cref="BranchTree"/>), so a region adds up its branches and their offices.
/// </remarks>
public static class BranchScope
{
    public static IQueryable<Project> InBranch(this IQueryable<Project> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(p => p.Branch!.Path.Contains(token));
    }

    public static IQueryable<Vehicle> InBranch(this IQueryable<Vehicle> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(v => v.Branch!.Path.Contains(token)
            || (v.BranchId == null && v.AssignedProject != null && v.AssignedProject.Branch!.Path.Contains(token)));
    }

    public static IQueryable<Tool> InBranch(this IQueryable<Tool> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(t => t.Branch!.Path.Contains(token)
            || (t.BranchId == null && t.AssignedProject != null && t.AssignedProject.Branch!.Path.Contains(token)));
    }

    public static IQueryable<GeneralExpense> InBranch(this IQueryable<GeneralExpense> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(e => e.Branch!.Path.Contains(token)
            || (e.BranchId == null && e.Project != null && e.Project.Branch!.Path.Contains(token)));
    }

    public static IQueryable<FinanceEntry> InBranch(this IQueryable<FinanceEntry> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(e => e.Branch!.Path.Contains(token)
            || (e.BranchId == null && e.Project != null && e.Project.Branch!.Path.Contains(token)));
    }

    /// <summary>Hours of one unit, counted by the site they were worked on or by the unit that employed the person.</summary>
    public static IQueryable<TimeEntry> InBranch(this IQueryable<TimeEntry> query, Guid? branchId, BranchBasis basis)
    {
        if (branchId is not { } id)
        {
            return query;
        }

        var token = BranchTree.Token(id);

        return basis == BranchBasis.Employer
            ? query.Where(t => t.Employee.BranchPeriods.Any(p => p.Branch.Path.Contains(token)
                && p.StartDate <= DateOnly.FromDateTime(t.StartedAt)
                && (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(t.StartedAt))))
            : query.Where(t => t.Project != null && t.Project.Branch!.Path.Contains(token));
    }

    /// <summary>Manual pay of one unit, counted by where it was booked or by the unit that employed the person that day.</summary>
    public static IQueryable<FinanceEntry> InBranch(this IQueryable<FinanceEntry> query, Guid? branchId, BranchBasis basis)
    {
        if (branchId is not { } id)
        {
            return query;
        }

        var token = BranchTree.Token(id);

        return basis == BranchBasis.Employer
            ? query.Where(e => e.Employee.BranchPeriods.Any(p => p.Branch.Path.Contains(token)
                && p.StartDate <= e.OccurredOn
                && (p.EndDate == null || p.EndDate >= e.OccurredOn)))
            : query.InBranch(branchId);
    }

    /// <summary>The employees a unit employs now: the open-ended period.</summary>
    public static IQueryable<Employee> InBranch(this IQueryable<Employee> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(e => e.BranchPeriods.Any(p => p.Branch.Path.Contains(token) && p.EndDate == null));
    }

    /// <summary>Absences of the people a unit employed on the day the absence began.</summary>
    public static IQueryable<Absence> InBranch(this IQueryable<Absence> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(a => a.Employee.BranchPeriods.Any(p => p.Branch.Path.Contains(token)
            && p.StartDate <= a.StartDate && (p.EndDate == null || p.EndDate >= a.StartDate)));
    }

    /// <summary>The ids of the people a unit employed at any time between two days, or null when no unit is asked for.</summary>
    public static async Task<List<Guid>?> EmployeeIdsAsync(
        IApplicationDbContext context, Guid? branchId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (branchId is not { } id)
        {
            return null;
        }

        var token = BranchTree.Token(id);

        return await context.EmployeeBranches
            .AsNoTracking()
            .Where(p => p.Branch.Path.Contains(token) && p.StartDate <= to && (p.EndDate == null || p.EndDate >= from))
            .Select(p => p.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public static IQueryable<CompanyRevenue> InBranch(this IQueryable<CompanyRevenue> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(r => r.Branch!.Path.Contains(token));
    }

    public static IQueryable<Accommodation> InBranch(this IQueryable<Accommodation> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(a => a.Branch!.Path.Contains(token));
    }

    public static IQueryable<MaterialMovement> InBranch(this IQueryable<MaterialMovement> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(m => m.Project != null && m.Project.Branch!.Path.Contains(token));
    }

    public static IQueryable<ProjectRevenue> InBranch(this IQueryable<ProjectRevenue> query, Guid? branchId)
    {
        if (branchId is not { } id) return query;
        var token = BranchTree.Token(id);
        return query.Where(r => r.Project.Branch!.Path.Contains(token));
    }

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
