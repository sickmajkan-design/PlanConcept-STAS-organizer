using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees;

/// <summary>
/// Keeps a tool or vehicle's project in step with wherever the employee
/// holding it currently is — "their gear follows them" — without touching the
/// tool/vehicle's own, independent "assign to a project" action.
/// </summary>
/// <remarks>
/// Only the equipment side moves. An employee's own project posting is the
/// source of truth; a tool or vehicle assigned directly to a project by
/// someone in the office (no employee attached) is never touched here.
/// </remarks>
public static class EmployeeEquipmentSync
{
    /// <summary>Moves everything this employee currently holds onto their new project.</summary>
    public static async Task FollowEmployeeAsync(
        IApplicationDbContext context,
        Guid employeeId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var tools = await context.Tools
            .Where(t => t.AssignedEmployeeId == employeeId && t.AssignedProjectId != projectId)
            .ToListAsync(cancellationToken);

        foreach (var tool in tools)
        {
            tool.AssignedProjectId = projectId;
        }

        var vehicles = await context.Vehicles
            .Where(v => v.AssignedEmployeeId == employeeId && v.AssignedProjectId != projectId)
            .ToListAsync(cancellationToken);

        foreach (var vehicle in vehicles)
        {
            vehicle.AssignedProjectId = projectId;
        }
    }

    /// <summary>
    /// Clears the project from whatever this employee holds, but only where it
    /// still matches the project they just left — a tool independently moved
    /// on to something else in the meantime is left alone.
    /// </summary>
    public static async Task ReleaseFromProjectAsync(
        IApplicationDbContext context,
        Guid employeeId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var tools = await context.Tools
            .Where(t => t.AssignedEmployeeId == employeeId && t.AssignedProjectId == projectId)
            .ToListAsync(cancellationToken);

        foreach (var tool in tools)
        {
            tool.AssignedProjectId = null;
        }

        var vehicles = await context.Vehicles
            .Where(v => v.AssignedEmployeeId == employeeId && v.AssignedProjectId == projectId)
            .ToListAsync(cancellationToken);

        foreach (var vehicle in vehicles)
        {
            vehicle.AssignedProjectId = null;
        }
    }
}
