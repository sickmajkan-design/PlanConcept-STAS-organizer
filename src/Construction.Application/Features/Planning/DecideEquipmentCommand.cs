using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Tools.Commands.AssignTool;
using Construction.Application.Features.Tools.Commands.UnassignTool;
using Construction.Application.Features.Vehicles.Commands.AssignVehicle;
using Construction.Application.Features.Vehicles.Commands.AssignVehicleToProject;
using Construction.Application.Features.Vehicles.Commands.UnassignVehicle;
using Construction.Application.Features.Vehicles.Commands.UnassignVehicleFromProject;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>What the planner decided about one vehicle or tool.</summary>
public enum EquipmentChoice
{
    /// <summary>It stays with whoever holds it. Nothing changes.</summary>
    Stay = 1,

    /// <summary>It goes to another person, and onto the site that person is posted to today.</summary>
    HandOver = 2,

    /// <summary>Back to the store: held by nobody, on no site.</summary>
    ToStore = 3,
}

public class EquipmentChoiceItem
{
    /// <summary>"Vehicle" or "Tool".</summary>
    public string Kind { get; init; } = null!;

    public Guid Id { get; init; }

    public EquipmentChoice Choice { get; init; }

    /// <summary>Who receives it. Required for <see cref="EquipmentChoice.HandOver"/>.</summary>
    public Guid? ToEmployeeId { get; init; }
}

/// <summary>
/// The planner's decision about equipment when someone leaves a site or is away: each item stays, goes to another person,
/// or goes back to the store. Returns how many items were changed.
/// </summary>
/// <remarks>
/// Built on the ordinary assign and unassign commands, so every rule they have (an item in service cannot be handed over,
/// the new holder is told, the caller must be allowed to touch the item's site) and the audit trail of who changed what
/// and when come with it. The items are applied one after another, each saved on its own: if one is refused the ones before
/// it stay done, and the screen reloads to show where things stand.
/// </remarks>
public record DecideEquipmentCommand : IRequest<int>
{
    public IReadOnlyList<EquipmentChoiceItem> Items { get; init; } = [];
}

public class DecideEquipmentCommandValidator : AbstractValidator<DecideEquipmentCommand>
{
    public DecideEquipmentCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Choose what to do with at least one item.");
        RuleFor(x => x.Items.Count).LessThanOrEqualTo(50).WithMessage("At most 50 items at once.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.Id).Distinct().Count() == items.Count)
            .WithMessage("The same item is listed twice.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEmpty();
            item.RuleFor(i => i.Kind).Must(k => k is "Vehicle" or "Tool").WithMessage("The kind must be Vehicle or Tool.");
            item.RuleFor(i => i.Choice).IsInEnum();
            item.RuleFor(i => i.ToEmployeeId).NotNull().WithMessage("Choose who receives it.")
                .When(i => i.Choice == EquipmentChoice.HandOver);
        });
    }
}

public class DecideEquipmentCommandHandler : IRequestHandler<DecideEquipmentCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISender _sender;

    public DecideEquipmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _sender = sender;
    }

    public async Task<int> Handle(DecideEquipmentCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);

        if (!access.CanEdit)
        {
            throw new ForbiddenAccessException("You may not change the schedule.");
        }

        var receivers = request.Items
            .Where(i => i is { Choice: EquipmentChoice.HandOver, ToEmployeeId: not null })
            .Select(i => i.ToEmployeeId!.Value)
            .Distinct()
            .ToList();

        if (receivers.Count > 0)
        {
            // A foreman may hand equipment only to people in their own unit.
            await PlanningRules.EnsureCanMoveAsync(_context, access, receivers, null, cancellationToken);
        }

        var today = _clock.Today;
        var changed = 0;

        foreach (var item in request.Items.Where(i => i.Choice != EquipmentChoice.Stay))
        {
            changed += item.Kind == "Vehicle"
                ? await DecideVehicleAsync(item, today, cancellationToken)
                : await DecideToolAsync(item, today, cancellationToken);
        }

        return changed;
    }

    private async Task<Guid?> PostedProjectAsync(Guid employeeId, DateOnly today, CancellationToken cancellationToken) =>
        await _context.EmployeeProjects
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId && p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))
            .OrderBy(p => p.StartDate)
            .Select(p => (Guid?)p.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int> DecideToolAsync(EquipmentChoiceItem item, DateOnly today, CancellationToken cancellationToken)
    {
        var tool = await _context.Tools
            .AsNoTracking()
            .Where(t => t.Id == item.Id)
            .Select(t => new { t.AssignedEmployeeId, t.AssignedProjectId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tool", item.Id);

        if (item.Choice == EquipmentChoice.ToStore)
        {
            if (tool.AssignedEmployeeId is not null)
            {
                await _sender.Send(new UnassignToolCommand(item.Id, ToolAssignmentTarget.Employee), cancellationToken);
            }

            if (tool.AssignedProjectId is not null)
            {
                await _sender.Send(new UnassignToolCommand(item.Id, ToolAssignmentTarget.Project), cancellationToken);
            }

            return tool.AssignedEmployeeId is null && tool.AssignedProjectId is null ? 0 : 1;
        }

        var to = item.ToEmployeeId!.Value;
        var changed = 0;

        if (tool.AssignedEmployeeId != to)
        {
            await _sender.Send(new AssignToolToEmployeeCommand(item.Id, to), cancellationToken);
            changed = 1;
        }

        var site = await PostedProjectAsync(to, today, cancellationToken);

        if (site is { } project)
        {
            if (tool.AssignedProjectId != project)
            {
                await _sender.Send(new AssignToolToProjectCommand(item.Id, project), cancellationToken);
                changed = 1;
            }
        }
        else if (tool.AssignedProjectId is not null)
        {
            await _sender.Send(new UnassignToolCommand(item.Id, ToolAssignmentTarget.Project), cancellationToken);
            changed = 1;
        }

        return changed;
    }

    private async Task<int> DecideVehicleAsync(EquipmentChoiceItem item, DateOnly today, CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.Id == item.Id)
            .Select(v => new { v.AssignedEmployeeId, v.AssignedProjectId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Vehicle", item.Id);

        if (item.Choice == EquipmentChoice.ToStore)
        {
            if (vehicle.AssignedEmployeeId is not null)
            {
                await _sender.Send(new UnassignVehicleCommand(item.Id), cancellationToken);
            }

            if (vehicle.AssignedProjectId is not null)
            {
                await _sender.Send(new UnassignVehicleFromProjectCommand(item.Id), cancellationToken);
            }

            return vehicle.AssignedEmployeeId is null && vehicle.AssignedProjectId is null ? 0 : 1;
        }

        var to = item.ToEmployeeId!.Value;
        var changed = 0;

        if (vehicle.AssignedEmployeeId != to)
        {
            await _sender.Send(new AssignVehicleCommand(item.Id, to), cancellationToken);
            changed = 1;
        }

        var site = await PostedProjectAsync(to, today, cancellationToken);

        if (site is { } project)
        {
            if (vehicle.AssignedProjectId != project)
            {
                await _sender.Send(new AssignVehicleToProjectCommand(item.Id, project), cancellationToken);
                changed = 1;
            }
        }
        else if (vehicle.AssignedProjectId is not null)
        {
            await _sender.Send(new UnassignVehicleFromProjectCommand(item.Id), cancellationToken);
            changed = 1;
        }

        return changed;
    }
}
