using Construction.Application.Common.Exceptions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.WorkItems;

/// <summary>
/// Who may move a work item where.
/// </summary>
public static class WorkItemRules
{
    /// <summary>
    /// The states each state may move to.
    /// </summary>
    /// <remarks>
    /// Stated as a table rather than as a chain of ifs so the whole lifecycle
    /// is readable at once, and so a state nobody can leave is visible as an
    /// empty row instead of hiding in a missing branch.
    ///
    /// Reopening is deliberately allowed from Resolved but not from Closed:
    /// "you have not actually fixed this" is an everyday correction, while
    /// reopening something signed off is a new item with its own record.
    /// </remarks>
    private static readonly IReadOnlyDictionary<WorkItemStatus, WorkItemStatus[]> Transitions =
        new Dictionary<WorkItemStatus, WorkItemStatus[]>
        {
            [WorkItemStatus.Open] =
            [
                WorkItemStatus.InProgress,
                WorkItemStatus.Resolved,
                WorkItemStatus.Cancelled
            ],
            [WorkItemStatus.InProgress] =
            [
                WorkItemStatus.Open,
                WorkItemStatus.Resolved,
                WorkItemStatus.Cancelled
            ],
            [WorkItemStatus.Resolved] =
            [
                WorkItemStatus.Closed,
                WorkItemStatus.Open,
                WorkItemStatus.InProgress
            ],
            [WorkItemStatus.Closed] = [],
            [WorkItemStatus.Cancelled] = []
        };

    public static bool CanTransition(WorkItemStatus from, WorkItemStatus to) =>
        from != to && Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static void EnsureTransitionAllowed(WorkItemStatus from, WorkItemStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new ConflictException(
                $"A {from} item cannot be moved to {to}.");
        }
    }

    /// <summary>
    /// Closing is a check that the work was actually done, so it is not the
    /// same person's call as doing it.
    /// </summary>
    public static bool CanClose(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman;

    /// <summary>
    /// True when this caller may change the item at all.
    /// </summary>
    /// <remarks>
    /// A Worker may move what is assigned to them and nothing else. That is
    /// the whole point of assigning it: they mark it started and done from the
    /// site, without being able to touch anyone else's list.
    /// </remarks>
    public static bool CanModify(UserRole? role, Guid? callerEmployeeId, WorkItem item)
    {
        if (role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman)
        {
            return true;
        }

        return role is UserRole.Worker
            && callerEmployeeId is not null
            && item.AssignedEmployeeId == callerEmployeeId;
    }

    /// <summary>
    /// True when this caller may create work.
    /// </summary>
    /// <remarks>
    /// Foreman and above create anything. A Worker may raise a defect and
    /// nothing else — reporting a crack in a wall is exactly what the person
    /// standing in front of it should be able to do, while handing out tasks
    /// is not.
    /// </remarks>
    public static bool CanCreate(UserRole? role, WorkItemKind kind) =>
        role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman
        || (role is UserRole.Worker && kind == WorkItemKind.Defect);

    /// <summary>Assigning work to somebody is a supervisor's call.</summary>
    public static bool CanAssign(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman;

    /// <summary>
    /// True for roles that only ever see their own work.
    /// </summary>
    public static bool IsRestrictedToOwnItems(UserRole? role) =>
        role is null or UserRole.Worker;
}
