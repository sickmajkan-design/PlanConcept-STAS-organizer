using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>
/// Everything the planning screen needs to draw any stretch of days: who is posted where, who is
/// away, and how many people of which position each site needs.
/// </summary>
/// <remarks>
/// Raw facts, not conclusions. Whether a site is short on a given day, and who could stand in, are
/// worked out in the browser from these, so a change made on screen shows at once without another
/// round trip and the same rules serve the screen and the leave-approval dialog.
/// </remarks>
public record GetPlanningQuery : IRequest<PlanningDto>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetPlanningQueryValidator : AbstractValidator<GetPlanningQuery>
{
    public GetPlanningQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < PlanningLimits.MaxDays)
            .WithMessage($"The planning window can cover at most {PlanningLimits.MaxDays} days.")
            .OverridePropertyName(nameof(GetPlanningQuery.To));
    }
}

public class PlanningDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public DateOnly Today { get; init; }

    public IReadOnlyList<PlanningProjectDto> Projects { get; init; } = [];

    public IReadOnlyList<PlanningEmployeeDto> Employees { get; init; } = [];

    /// <summary>Every position an employee currently holds, to offer when a project's needs are edited.</summary>
    public IReadOnlyList<string> Positions { get; init; } = [];
}

public class PlanningProjectDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Status { get; init; } = null!;

    public string? CustomerName { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public bool WorksSaturdays { get; init; }

    public bool WorksSundays { get; init; }

    public IReadOnlyList<PlanningNeedDto> Needs { get; init; } = [];
}

public class PlanningNeedDto
{
    public string Position { get; init; } = null!;

    public int Count { get; init; }
}

public class PlanningEmployeeDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;

    public IReadOnlyList<PlanningPostingDto> Postings { get; init; } = [];

    public IReadOnlyList<PlanningAbsenceDto> Absences { get; init; } = [];
}

public class PlanningPostingDto
{
    public Guid ProjectId { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>When the worker confirmed it on their phone. Null while they have not.</summary>
    public DateTime? AcknowledgedAt { get; init; }
}

public class PlanningAbsenceDto
{
    public string Type { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }
}

public class GetPlanningQueryHandler : IRequestHandler<GetPlanningQuery, PlanningDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public GetPlanningQueryHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<PlanningDto> Handle(GetPlanningQuery request, CancellationToken cancellationToken)
    {
        var from = request.From;
        var to = request.To;

        var employees = await _context.Employees
            .AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new
            {
                e.Id,
                FullName = e.FirstName + " " + e.LastName,
                e.Position,
                Postings = e.ProjectAssignments
                    .Where(a => a.StartDate <= to && (a.EndDate == null || a.EndDate >= from))
                    .Select(a => new PlanningPostingDto { ProjectId = a.ProjectId, StartDate = a.StartDate, EndDate = a.EndDate, AcknowledgedAt = a.AcknowledgedAt })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var ids = employees.Select(e => e.Id).ToList();

        // Approved only: a request is a question, not a fact the plan can lean on.
        var absences = await _context.Absences
            .AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId)
                && a.Status == AbsenceStatus.Approved
                && a.StartDate <= to && a.EndDate >= from)
            .Select(a => new { a.EmployeeId, a.Type, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var postedProjectIds = employees.SelectMany(e => e.Postings).Select(p => p.ProjectId).Distinct().ToList();

        var projects = await _context.Projects
            .AsNoTracking()
            .InBranch(request.BranchId)
            .Where(p => (p.Status == ProjectStatus.Planned || p.Status == ProjectStatus.Active || p.Status == ProjectStatus.OnHold)
                    && (p.EndDate == null || p.EndDate >= from)
                    && (p.StartDate == null || p.StartDate <= to)
                || postedProjectIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .Select(p => new PlanningProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status.ToString(),
                CustomerName = p.Customer != null ? p.Customer.Name : null,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                WorksSaturdays = p.WorksSaturdays,
                WorksSundays = p.WorksSundays,
                Needs = p.StaffingNeeds
                    .OrderBy(n => n.Position)
                    .Select(n => new PlanningNeedDto { Position = n.Position, Count = n.Count })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var positions = employees
            .Select(e => e.Position.Trim())
            .Where(p => p.Length > 0)
            .GroupBy(PositionKey.Of)
            .Select(g => g.First())
            .OrderBy(p => p)
            .ToList();

        return new PlanningDto
        {
            From = from,
            To = to,
            Today = DateOnly.FromDateTime(_clock.UtcNow),
            Projects = projects,
            Positions = positions,
            Employees = employees.Select(e => new PlanningEmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                Position = e.Position.Trim(),
                Postings = e.Postings,
                Absences = absences
                    .Where(a => a.EmployeeId == e.Id)
                    .Select(a => new PlanningAbsenceDto { Type = a.Type.ToString(), StartDate = a.StartDate, EndDate = a.EndDate })
                    .ToList(),
            }).ToList(),
        };
    }
}
