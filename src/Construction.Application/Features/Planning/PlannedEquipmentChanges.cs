using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Entities;
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
public static class PlannedEquipmentApplier
{
    /// <summary>Applies every change due on or before <paramref name="today"/>, oldest first. Returns how many were applied.</summary>
    public static async Task<int> ApplyDueAsync(IApplicationDbContext context, DateOnly today, CancellationToken cancellationToken)
    {
        var due = await context.PlannedEquipmentChanges
            .Where(c => c.EffectiveFrom <= today)
            .OrderBy(c => c.EffectiveFrom).ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var change in due)
        {
            if (change.Kind == EquipmentKind.Vehicle)
            {
                var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == change.ItemId, cancellationToken);

                if (vehicle is not null)
                {
                    vehicle.AssignedEmployeeId = change.EmployeeId;
                    vehicle.AssignedProjectId = change.ProjectId;
                }
            }
            else
            {
                var tool = await context.Tools.FirstOrDefaultAsync(t => t.Id == change.ItemId, cancellationToken);

                if (tool is not null)
                {
                    tool.AssignedEmployeeId = change.EmployeeId;
                    tool.AssignedProjectId = change.ProjectId;
                }
            }

            context.PlannedEquipmentChanges.Remove(change);
            await context.SaveChangesAsync(cancellationToken);
        }

        return due.Count;
    }
}
