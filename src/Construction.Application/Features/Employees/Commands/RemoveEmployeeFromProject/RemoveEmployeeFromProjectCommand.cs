using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Employees;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.RemoveEmployeeFromProject;

/// <summary>Takes an employee off a project.</summary>
/// <remarks>
/// Once postings have dates, "remove" splits into two different acts. A
/// posting that has not started yet was a plan, and deleting it is right. One
/// already under way is history — the person was on that site — so it is
/// closed off as of today rather than erased. Deleting it would make the
/// schedule disagree with the timesheets it sits next to.
/// </remarks>
public record RemoveEmployeeFromProjectCommand(Guid EmployeeId, Guid ProjectId) : IRequest;

public class RemoveEmployeeFromProjectCommandHandler
    : IRequestHandler<RemoveEmployeeFromProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveEmployeeFromProjectCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(
        RemoveEmployeeFromProjectCommand request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        // The current posting, or the next one due to start. Ordering by start
        // date makes "remove them from this site" mean the same thing whether
        // there is one posting or several.
        var assignment = await _context.EmployeeProjects
            .Where(ep => ep.EmployeeId == request.EmployeeId
                && ep.ProjectId == request.ProjectId
                && (ep.EndDate == null || ep.EndDate >= today))
            .OrderBy(ep => ep.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(
                $"Employee '{request.EmployeeId}' is not posted to project '{request.ProjectId}'.");

        if (assignment.StartDate > today)
        {
            // Never happened; nothing to preserve.
            _context.EmployeeProjects.Remove(assignment);
        }
        else
        {
            assignment.EndDate = today;
        }

        // Their gear either follows them to wherever else they are currently
        // posted, or comes back off this project if nowhere else claims it.
        // An employee can be posted to more than one site at once — a
        // supervisor covering two jobs is a real case the board already
        // allows — so when several other postings are still active, the one
        // most recently assigned wins. That is the site the office most
        // recently decided they belong to, and it is the only ordering that
        // does not depend on how the database happened to hand rows back.
        var otherActiveProjectId = await _context.EmployeeProjects
            .Where(ep => ep.EmployeeId == request.EmployeeId
                && ep.ProjectId != request.ProjectId
                && ep.StartDate <= today
                && (ep.EndDate == null || ep.EndDate >= today))
            .OrderByDescending(ep => ep.AssignedAt)
            .Select(ep => (Guid?)ep.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        if (otherActiveProjectId is { } otherProjectId)
        {
            await EmployeeEquipmentSync.FollowEmployeeAsync(
                _context, request.EmployeeId, otherProjectId, cancellationToken);
        }
        else
        {
            await EmployeeEquipmentSync.ReleaseFromProjectAsync(
                _context, request.EmployeeId, request.ProjectId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
