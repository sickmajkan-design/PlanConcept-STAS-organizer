using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Queries.GetSchedule;

/// <summary>
/// Who is posted where, and who is away, across a window.
/// </summary>
/// <remarks>
/// One query rather than a call per employee. A fifty-person crew over a week
/// is otherwise a hundred round trips to draw one screen, and the screen is
/// the point of the module.
///
/// Only approved absences appear. A request that has not been answered is a
/// question, and a board that greys someone out on the strength of one would
/// have work planned around leave nobody granted.
///
/// A worker gets the same query narrowed to their own line, which is what the
/// phone shows: where am I this week. Narrowed rather than refused, so the
/// answer to "whose schedule?" is always their own.
/// </remarks>
public record GetScheduleQuery : IRequest<ScheduleDto>
{
    /// <summary>Widest window the board will draw. Six weeks covers a quarter's planning.</summary>
    public const int MaxDays = 42;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>Narrows the board to one site.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// When true, leaves out employees with nothing on them in this window.
    /// </summary>
    public bool AssignedOnly { get; init; }
}

public class GetScheduleQueryValidator : AbstractValidator<GetScheduleQuery>
{
    public GetScheduleQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The end of the window must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetScheduleQuery.MaxDays)
            .WithMessage($"The window must not exceed {GetScheduleQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetScheduleQueryHandler : IRequestHandler<GetScheduleQuery, ScheduleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetScheduleQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ScheduleDto> Handle(
        GetScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var from = request.From;
        var to = request.To;

        // A worker sees their own line only. An account not linked to an
        // employee has no line to see, so it gets an empty board rather than
        // everybody's.
        var restrictedTo = AbsenceRules.IsRestrictedToOwnAbsences(_currentUserService.Role)
            ? _currentUserService.EmployeeId ?? Guid.Empty
            : (Guid?)null;

        if (restrictedTo == Guid.Empty)
        {
            return new ScheduleDto { From = from, To = to };
        }

        // Overlap, not containment: a posting that started last month and runs
        // through this week belongs on the board.
        var assignments = await _context.EmployeeProjects
            .AsNoTracking()
            .Where(ep => restrictedTo == null || ep.EmployeeId == restrictedTo)
            .Where(ep => ep.StartDate <= to && (ep.EndDate == null || ep.EndDate >= from))
            .Where(ep => request.ProjectId == null || ep.ProjectId == request.ProjectId)
            .Select(ep => new
            {
                ep.Id,
                ep.EmployeeId,
                ep.ProjectId,
                ProjectName = ep.Project.Name,
                ep.StartDate,
                ep.EndDate
            })
            .ToListAsync(cancellationToken);

        var absences = await _context.Absences
            .AsNoTracking()
            .Where(a => restrictedTo == null || a.EmployeeId == restrictedTo)
            .Where(a => a.Status == AbsenceStatus.Approved)
            .Where(a => a.StartDate <= to && a.EndDate >= from)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.Type,
                a.StartDate,
                a.EndDate
            })
            .ToListAsync(cancellationToken);

        var employeeIds = assignments.Select(a => a.EmployeeId)
            .Concat(absences.Select(a => a.EmployeeId))
            .ToHashSet();

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => restrictedTo == null || e.Id == restrictedTo)
            // The unassigned still belong on the board — they are exactly who
            // a supervisor is looking for when filling a gap.
            .Where(e => !request.AssignedOnly || employeeIds.Contains(e.Id))
            .Where(e => e.Status == EmployeeStatus.Active || employeeIds.Contains(e.Id))
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new
            {
                e.Id,
                Name = e.FirstName + " " + e.LastName,
                e.Position
            })
            .ToListAsync(cancellationToken);

        var assignmentsByEmployee = assignments.ToLookup(a => a.EmployeeId);
        var absencesByEmployee = absences.ToLookup(a => a.EmployeeId);

        var rows = employees.Select(employee => new ScheduleRowDto
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.Name,
            Position = employee.Position,
            Assignments = assignmentsByEmployee[employee.Id]
                .OrderBy(a => a.StartDate)
                .Select(a => new ScheduleAssignmentDto
                {
                    Id = a.Id,
                    ProjectId = a.ProjectId,
                    ProjectName = a.ProjectName,
                    // Clipped to the window so the client draws a bar without
                    // repeating this arithmetic.
                    From = a.StartDate > from ? a.StartDate : from,
                    To = a.EndDate is { } end && end < to ? end : to,
                    ContinuesAfter = a.EndDate is null || a.EndDate > to
                })
                .ToList(),
            Absences = absencesByEmployee[employee.Id]
                .OrderBy(a => a.StartDate)
                .Select(a => new ScheduleAbsenceDto
                {
                    Id = a.Id,
                    Type = a.Type,
                    From = a.StartDate > from ? a.StartDate : from,
                    To = a.EndDate < to ? a.EndDate : to
                })
                .ToList()
        }).ToList();

        return new ScheduleDto { From = from, To = to, Rows = rows };
    }
}
