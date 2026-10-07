using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Audit.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>Who changed the schedule and when: the postings made, changed and removed, newest first.</summary>
/// <remarks>
/// Read out of the audit trail, so it is as complete as the trail is. A foreman sees only people of their
/// own business unit, the same limit the schedule itself has.
/// </remarks>
public record GetPlanningHistoryQuery : IRequest<IReadOnlyList<PlanningHistoryDto>>
{
    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    /// <summary>How many entries to return, 1–200.</summary>
    public int Take { get; init; } = 50;
}

public class GetPlanningHistoryQueryValidator : AbstractValidator<GetPlanningHistoryQuery>
{
    public GetPlanningHistoryQueryValidator()
    {
        RuleFor(x => x.Take).InclusiveBetween(1, 200);
    }
}

public class PlanningHistoryDto
{
    public long Id { get; init; }

    public DateTime OccurredAt { get; init; }

    /// <summary><c>Created</c>, <c>Updated</c> or <c>Deleted</c>.</summary>
    public string Action { get; init; } = null!;

    public Guid? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    /// <summary>The days of the posting after the change (or before it, when it was removed).</summary>
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>Who did it, as their account was at the time; null for the system.</summary>
    public string? ByEmail { get; init; }

    /// <summary>For a change: which of the start and end moved, with before and after.</summary>
    public IReadOnlyDictionary<string, AuditChangeDto> Changes { get; init; } = new Dictionary<string, AuditChangeDto>();
}

public class GetPlanningHistoryQueryHandler : IRequestHandler<GetPlanningHistoryQuery, IReadOnlyList<PlanningHistoryDto>>
{
    private static readonly string[] Shown = ["StartDate", "EndDate"];

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetPlanningHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PlanningHistoryDto>> Handle(GetPlanningHistoryQuery request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);

        // The trail is read in a generous window and narrowed after names are known, since a change of days
        // carries neither the person nor the site.
        var entries = await _context.AuditEntries.AsNoTracking()
            .Where(a => a.EntityName == nameof(Domain.Entities.EmployeeProject))
            .OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id)
            .Take(1000)
            .Select(AuditEntryMapping.Projection)
            .ToListAsync(cancellationToken);

        var postingIds = entries.Select(e => e.EntityId).Distinct().ToList();

        var known = await _context.EmployeeProjects.IgnoreQueryFilters().AsNoTracking()
            .Where(p => postingIds.Contains(p.Id))
            .Select(p => new { p.Id, p.EmployeeId, p.ProjectId })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        Guid? IdIn(AuditEntryDto e, string key) =>
            e.Changes.TryGetValue(key, out var c) && Guid.TryParse(c.To ?? c.From, out var id) ? id : null;

        var resolved = entries
            .Select(e => new
            {
                Entry = e,
                EmployeeId = known.TryGetValue(e.EntityId, out var k) ? k.EmployeeId : IdIn(e, "EmployeeId"),
                ProjectId = known.TryGetValue(e.EntityId, out k) ? k.ProjectId : IdIn(e, "ProjectId"),
            })
            .Where(r => request.EmployeeId is null || r.EmployeeId == request.EmployeeId)
            .Where(r => request.ProjectId is null || r.ProjectId == request.ProjectId)
            .ToList();

        if (access.IsScoped)
        {
            var unit = access.BranchId!.Value;
            var mine = await _context.EmployeeBranches.AsNoTracking()
                .Where(p => p.BranchId == unit && p.EndDate == null)
                .Select(p => p.EmployeeId)
                .ToListAsync(cancellationToken);
            resolved = resolved.Where(r => r.EmployeeId is { } id && mine.Contains(id)).ToList();
        }

        resolved = resolved.Take(request.Take).ToList();

        var employeeIds = resolved.Select(r => r.EmployeeId).OfType<Guid>().Distinct().ToList();
        var projectIds = resolved.Select(r => r.ProjectId).OfType<Guid>().Distinct().ToList();

        var employees = await _context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

        var projects = await _context.Projects.IgnoreQueryFilters().AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        DateOnly? Day(AuditEntryDto e, string key) =>
            e.Changes.TryGetValue(key, out var c) && DateOnly.TryParse(c.To ?? c.From, out var d) ? d : null;

        return resolved
            .Select(r => new PlanningHistoryDto
            {
                Id = r.Entry.Id,
                OccurredAt = r.Entry.OccurredAt,
                Action = r.Entry.Action,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.EmployeeId is { } e && employees.TryGetValue(e, out var en) ? en : null,
                ProjectId = r.ProjectId,
                ProjectName = r.ProjectId is { } p && projects.TryGetValue(p, out var pn) ? pn : null,
                StartDate = Day(r.Entry, "StartDate"),
                EndDate = Day(r.Entry, "EndDate"),
                ByEmail = r.Entry.UserEmail,
                Changes = r.Entry.Changes.Where(c => Shown.Contains(c.Key)).ToDictionary(c => c.Key, c => c.Value),
            })
            .ToList();
    }
}
