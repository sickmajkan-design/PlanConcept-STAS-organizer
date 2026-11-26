using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Tools.Commands;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>Takes back a planned change of equipment before its day.</summary>
public record CancelPlannedEquipmentChangeCommand(Guid Id) : IRequest;

public class CancelPlannedEquipmentChangeCommandHandler : IRequestHandler<CancelPlannedEquipmentChangeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CancelPlannedEquipmentChangeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelPlannedEquipmentChangeCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);

        if (!access.CanEdit)
        {
            throw new ForbiddenAccessException("You may not change the schedule.");
        }

        var change = await _context.PlannedEquipmentChanges.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(PlannedEquipmentChange), request.Id);

        // A foreman may only touch what is in their own unit.
        var inUnit = !access.IsScoped
            || (change.Kind == EquipmentKind.Vehicle
                ? await _context.Vehicles.InBranch(access.BranchId).AnyAsync(v => v.Id == change.ItemId, cancellationToken)
                : await _context.Tools.InBranch(access.BranchId).AnyAsync(t => t.Id == change.ItemId, cancellationToken));

        if (!inUnit)
        {
            throw new ForbiddenAccessException("That equipment is not in your unit.");
        }

        _context.PlannedEquipmentChanges.Remove(change);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Puts the planned changes whose day has come into effect on the items themselves.</summary>
/// <remarks>
/// Does what the office does by hand: the status follows the holder, and the new holder is told. Two cases are dropped
/// instead of applied, and the planner who made the change is told why: the item has been changed by hand since the plan
/// was made (the plan is out of date), or it cannot be handed over now (in service, under repair, lost).
/// </remarks>
public static class PlannedEquipmentApplier
{
    /// <summary>Applies every change due on or before <paramref name="today"/>, oldest first. Returns how many were applied.</summary>
    public static async Task<int> ApplyDueAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var due = await context.PlannedEquipmentChanges
            .Where(c => c.EffectiveFrom <= today)
            .OrderBy(c => c.EffectiveFrom).ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var applied = 0;

        foreach (var change in due)
        {
            var (label, reason) = await ApplyAsync(context, change, cancellationToken);

            context.PlannedEquipmentChanges.Remove(change);
            await context.SaveChangesAsync(cancellationToken);

            if (reason is null)
            {
                applied++;
                await NotifyReceiverAsync(context, notifications, change, label, cancellationToken);
            }
            else if (change.CreatedByUserId is { } planner)
            {
                await notifications.NotifyUserAsync(
                    planner,
                    NotificationType.GeneralAnnouncement,
                    "Planned equipment change not applied",
                    $"The planned change of '{label}' was not applied: {reason}",
                    cancellationToken: cancellationToken);
            }
        }

        return applied;
    }

    /// <summary>Returns the item's name and, when the change was not applied, why.</summary>
    private static async Task<(string Label, string? Reason)> ApplyAsync(
        IApplicationDbContext context, PlannedEquipmentChange change, CancellationToken cancellationToken)
    {
        // Changed by hand after the plan was made: the plan no longer describes what the office wants.
        var changedSince = await context.EquipmentHolds
            .AnyAsync(h => h.Kind == change.Kind && h.ItemId == change.ItemId && h.CreatedAt > change.CreatedAt, cancellationToken);

        if (change.Kind == EquipmentKind.Vehicle)
        {
            var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == change.ItemId, cancellationToken);

            if (vehicle is null)
            {
                return ("?", "the vehicle no longer exists.");
            }

            var label = $"{vehicle.Brand} {vehicle.Model} ({vehicle.RegistrationNumber})";

            if (changedSince)
            {
                return (label, "it was changed by hand after the plan was made.");
            }

            if (change.EmployeeId is not null && vehicle.Status is VehicleStatus.InService or VehicleStatus.OutOfService or VehicleStatus.RentedOut)
            {
                return (label, $"its status is '{vehicle.Status}'.");
            }

            vehicle.AssignedEmployeeId = change.EmployeeId;
            vehicle.AssignedProjectId = change.ProjectId;

            if (change.EmployeeId is not null)
            {
                vehicle.Status = VehicleStatus.Assigned;
            }
            else if (vehicle.Status == VehicleStatus.Assigned)
            {
                vehicle.Status = VehicleStatus.Available;
            }

            return (label, null);
        }

        var tool = await context.Tools.FirstOrDefaultAsync(t => t.Id == change.ItemId, cancellationToken);

        if (tool is null)
        {
            return ("?", "the tool no longer exists.");
        }

        if (changedSince)
        {
            return (tool.Name, "it was changed by hand after the plan was made.");
        }

        if (change.EmployeeId is not null && tool.Status is ToolStatus.UnderRepair or ToolStatus.Lost or ToolStatus.Retired or ToolStatus.RentedOut)
        {
            return (tool.Name, $"its status is '{tool.Status}'.");
        }

        tool.AssignedEmployeeId = change.EmployeeId;
        tool.AssignedProjectId = change.ProjectId;
        ToolRules.RecomputeStatus(tool);

        return (tool.Name, null);
    }

    private static async Task NotifyReceiverAsync(
        IApplicationDbContext context, INotificationService notifications, PlannedEquipmentChange change, string label, CancellationToken cancellationToken)
    {
        if (change.EmployeeId is not { } employeeId)
        {
            return;
        }

        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.IsActive, cancellationToken);

        if (user is null)
        {
            return;
        }

        var tool = change.Kind == EquipmentKind.Tool;

        await notifications.NotifyUserAsync(
            user.Id,
            tool ? NotificationType.ToolAssigned : NotificationType.VehicleAssigned,
            tool ? "Tool assigned" : "Vehicle assigned",
            $"{(tool ? "Tool" : "Vehicle")} {label} has been assigned to you.",
            new Dictionary<string, string> { [tool ? "toolId" : "vehicleId"] = change.ItemId.ToString() },
            cancellationToken: cancellationToken);
    }
}
