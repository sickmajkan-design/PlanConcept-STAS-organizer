using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Assignments.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Assignments.Queries.GetAssignmentBoard;

/// <summary>
/// The data behind the drag-and-drop assignment board: who is available, what
/// sites are open, and today's postings between them.
/// </summary>
/// <remarks>
/// One query rather than a fetch per side. The board draws every employee
/// against every project at once, so paging either side would mean the office
/// could not see, in one glance, who is free — which is the whole point of
/// the screen.
/// </remarks>
public record GetAssignmentBoardQuery : IRequest<AssignmentBoardDto>;

public class GetAssignmentBoardQueryHandler
    : IRequestHandler<GetAssignmentBoardQuery, AssignmentBoardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAssignmentBoardQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AssignmentBoardDto> Handle(
        GetAssignmentBoardQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new AssignmentBoardEmployeeDto
            {
                Id = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                EmployeeNumber = e.EmployeeNumber,
                Position = e.Position,
                // Open-ended, not "covers today": removing someone closes
                // their posting off as of today rather than deleting it, so
                // it still legitimately covers today's date — but the board
                // is a staffing tool, not a timesheet, and a posting the
                // office just closed has to disappear from it immediately,
                // not tomorrow. EndDate == null is exactly "still posted
                // there with no removal recorded."
                ProjectIds = e.ProjectAssignments
                    .Where(a => a.StartDate <= today && a.EndDate == null)
                    .Select(a => a.ProjectId)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Status == ProjectStatus.Planned
                || p.Status == ProjectStatus.Active
                || p.Status == ProjectStatus.OnHold)
            .OrderBy(p => p.Name)
            .Select(p => new AssignmentBoardProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return new AssignmentBoardDto { Employees = employees, Projects = projects };
    }
}
