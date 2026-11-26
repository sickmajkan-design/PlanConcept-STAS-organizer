using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>
/// Who held each vehicle and tool on one day, for the planning screen. Today is the present state; an earlier day comes from
/// the history (items with no history that far back are left out: nobody knows); a later day is the present state with the
/// planned changes applied.
/// </summary>
public record GetPlanningEquipmentQuery : IRequest<PlanningEquipmentOnDto>
{
    public DateOnly Date { get; init; }

    public Guid? BranchId { get; init; }
}

public class PlanningEquipmentOnDto
{
    public DateOnly Date { get; init; }

    public IReadOnlyList<PlanningHolderDto> Items { get; init; } = [];

    /// <summary>Every change still waiting for its day, soonest first, whatever day is asked about.</summary>
    public IReadOnlyList<PlannedEquipmentChangeDto> Planned { get; init; } = [];
}

public class PlanningHolderDto
{
    public string Kind { get; init; } = null!;

    public Guid Id { get; init; }

    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }
}

public class PlannedEquipmentChangeDto
{
    public Guid Id { get; init; }

    public string Kind { get; init; } = null!;

    public Guid ItemId { get; init; }

    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
}

public class GetPlanningEquipmentQueryHandler : IRequestHandler<GetPlanningEquipmentQuery, PlanningEquipmentOnDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;

    public GetPlanningEquipmentQueryHandler(IApplicationDbContext context, IDateTimeProvider clock, ICurrentUserService currentUser)
    {
        _context = context;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<PlanningEquipmentOnDto> Handle(GetPlanningEquipmentQuery request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        var branchId = access.IsScoped ? access.BranchId : request.BranchId;
        var today = _clock.Today;
        var date = request.Date;

        var vehicles = await _context.Vehicles.AsNoTracking().InBranch(branchId)
            .Select(v => new PlanningHolderDto { Kind = "Vehicle", Id = v.Id, EmployeeId = v.AssignedEmployeeId, ProjectId = v.AssignedProjectId })
            .ToListAsync(cancellationToken);
        var tools = await _context.Tools.AsNoTracking().InBranch(branchId)
            .Select(t => new PlanningHolderDto { Kind = "Tool", Id = t.Id, EmployeeId = t.AssignedEmployeeId, ProjectId = t.AssignedProjectId })
            .ToListAsync(cancellationToken);
        var present = vehicles.Concat(tools).ToList();
        var ids = present.Select(i => i.Id).ToList();

        var planned = await _context.PlannedEquipmentChanges.AsNoTracking()
            .Where(c => ids.Contains(c.ItemId))
            .OrderBy(c => c.EffectiveFrom).ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        List<PlanningHolderDto> items;

        if (date < today)
        {
            var holds = await _context.EquipmentHolds.AsNoTracking()
                .Where(h => h.From <= date && (h.To == null || h.To >= date) && ids.Contains(h.ItemId))
                .ToListAsync(cancellationToken);

            items = holds
                .GroupBy(h => (h.Kind, h.ItemId))
                .Select(g => g.OrderByDescending(h => h.From).First())
                .Select(h => new PlanningHolderDto { Kind = h.Kind.ToString(), Id = h.ItemId, EmployeeId = h.EmployeeId, ProjectId = h.ProjectId })
                .ToList();
        }
        else
        {
            items = present;

            if (date > today)
            {
                var latest = planned
                    .Where(c => c.EffectiveFrom <= date)
                    .GroupBy(c => (c.Kind, c.ItemId))
                    .ToDictionary(g => g.Key, g => g.Last());

                items = present
                    .Select(i => latest.TryGetValue((i.Kind == "Vehicle" ? EquipmentKind.Vehicle : EquipmentKind.Tool, i.Id), out var c)
                        ? new PlanningHolderDto { Kind = i.Kind, Id = i.Id, EmployeeId = c.EmployeeId, ProjectId = c.ProjectId }
                        : i)
                    .ToList();
            }
        }

        return new PlanningEquipmentOnDto
        {
            Date = date,
            Items = items,
            Planned = planned.Select(c => new PlannedEquipmentChangeDto
            {
                Id = c.Id,
                Kind = c.Kind.ToString(),
                ItemId = c.ItemId,
                EmployeeId = c.EmployeeId,
                ProjectId = c.ProjectId,
                EffectiveFrom = c.EffectiveFrom,
            }).ToList(),
        };
    }
}
