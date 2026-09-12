using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Queries.GetMyTeamTimeEntriesToday;

/// <summary>
/// Who clocked in and out today, for a Foreman's own site(s) — read-only, so
/// they can check the app's record against whatever they track on paper.
/// Approving hours is a separate, stricter action; this is just visibility.
/// </summary>
/// <remarks>
/// Mirrors <c>GetMyReportableProjectsQuery</c>'s own branch exactly: a
/// Foreman is scoped to sites they are currently posted to, and everyone
/// else — who has no need for this narrowing — sees every entry for the day,
/// the same "fall through to company-wide" default that query already uses.
/// </remarks>
public record GetMyTeamTimeEntriesTodayQuery : IRequest<IReadOnlyList<TimeEntryDto>>
{
    /// <summary>Defaults to today.</summary>
    public DateOnly? Date { get; init; }
}

public class GetMyTeamTimeEntriesTodayQueryHandler
    : IRequestHandler<GetMyTeamTimeEntriesTodayQuery, IReadOnlyList<TimeEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetMyTeamTimeEntriesTodayQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<TimeEntryDto>> Handle(
        GetMyTeamTimeEntriesTodayQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var date = request.Date ?? today;

        var query = _context.TimeEntries.AsNoTracking();

        if (_currentUserService.Role is UserRole.Foreman)
        {
            var employeeId = _currentUserService.EmployeeId;

            var projectIds = await _context.EmployeeProjects
                .Where(ep => ep.EmployeeId == employeeId
                    && ep.StartDate <= today
                    && (ep.EndDate == null || ep.EndDate >= today))
                .Select(ep => ep.ProjectId)
                .ToListAsync(cancellationToken);

            query = query.Where(t => t.ProjectId != null && projectIds.Contains(t.ProjectId!.Value));
        }

        query = query.Where(t => DateOnly.FromDateTime(t.StartedAt) == date);

        return await query
            .OrderBy(t => t.StartedAt)
            .Select(TimeEntryMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
