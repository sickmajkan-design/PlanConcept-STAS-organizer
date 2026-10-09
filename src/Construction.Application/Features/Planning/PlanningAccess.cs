using Construction.Application.Features.Branches;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>What the caller may do on the schedule, and over whom.</summary>
/// <param name="CanEdit">Whether they may move people between sites.</param>
/// <param name="IsScoped">True for a foreman: they see, and may move, only their own business unit.</param>
/// <param name="BranchId">The unit a scoped caller is limited to. <see cref="Guid.Empty"/> when they belong to none, which matches nobody.</param>
/// <param name="BranchName">The name of that unit, for the screen to say so.</param>
public sealed record PlanningAccess(bool CanEdit, bool IsScoped, Guid? BranchId, string? BranchName);

/// <summary>
/// Who may read and who may change the schedule. A project manager and above see and move everyone. A
/// foreman sees their own business unit and nothing else, and moves people only when an administrator has
/// granted them that on their account, and then only people and sites of that same unit.
/// </summary>
public static class PlanningRules
{
    public static async Task<PlanningAccess> ResolveAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        switch (currentUser.Role)
        {
            case UserRole.SuperAdmin or UserRole.Admin or UserRole.ProjectManager:
                return new PlanningAccess(true, false, null, null);

            case UserRole.Foreman:
                var granted = currentUser.UserId is { } userId
                    && await context.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.CanPlan, cancellationToken);

                var unit = currentUser.EmployeeId is { } employeeId
                    ? await context.EmployeeBranches.AsNoTracking()
                        .Where(p => p.EmployeeId == employeeId && p.EndDate == null)
                        .Select(p => new { p.BranchId, p.Branch.Name })
                        .FirstOrDefaultAsync(cancellationToken)
                    : null;

                return new PlanningAccess(granted, true, unit?.BranchId ?? Guid.Empty, unit?.Name);

            default:
                throw new ForbiddenAccessException("You may not use the schedule.");
        }
    }

    /// <summary>Refuses a change the caller may not make: no right to plan, or somebody outside their unit.</summary>
    public static async Task EnsureCanMoveAsync(
        IApplicationDbContext context,
        PlanningAccess access,
        IReadOnlyCollection<Guid> employeeIds,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        if (!access.CanEdit)
        {
            throw new ForbiddenAccessException("You may not change the schedule.");
        }

        if (!access.IsScoped)
        {
            return;
        }

        var branchId = access.BranchId!.Value;

        var inUnit = await context.EmployeeBranches.AsNoTracking()
            .Where(p => p.Branch.Path.Contains(BranchTree.Token(branchId)) && p.EndDate == null && employeeIds.Contains(p.EmployeeId))
            .Select(p => p.EmployeeId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (inUnit != employeeIds.Distinct().Count())
        {
            throw new ForbiddenAccessException("You may only move people of your own business unit.");
        }

        if (projectId is { } site && !await context.Projects.AsNoTracking().AnyAsync(p => p.Id == site && p.Branch!.Path.Contains(BranchTree.Token(branchId)), cancellationToken))
        {
            throw new ForbiddenAccessException("You may only post people to sites of your own business unit.");
        }
    }

    /// <summary>The needs of a project are set by a project manager and above, never by a foreman, granted or not.</summary>
    public static void EnsureNotScoped(PlanningAccess access)
    {
        if (access.IsScoped)
        {
            throw new ForbiddenAccessException("You may not change what a project needs.");
        }
    }
}
