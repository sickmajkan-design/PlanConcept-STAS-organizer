using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetMyReportableProjects;

public record ReportableProjectDto(Guid Id, string Name);

/// <summary>
/// The sites a weekly report could be submitted for, from the caller's own
/// point of view: a Foreman sees only where they are currently posted — the
/// same "raised it or assigned to it" narrowing every worker-facing screen in
/// this product already uses — everyone above Foreman sees every active site,
/// since they may be filing on a foreman's behalf.
/// </summary>
public record GetMyReportableProjectsQuery : IRequest<IReadOnlyList<ReportableProjectDto>>;

public class GetMyReportableProjectsQueryHandler
    : IRequestHandler<GetMyReportableProjectsQuery, IReadOnlyList<ReportableProjectDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetMyReportableProjectsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<ReportableProjectDto>> Handle(
        GetMyReportableProjectsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var query = _context.Projects.AsNoTracking()
            .Where(p => p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled);

        if (_currentUserService.Role is UserRole.Foreman or UserRole.Worker)
        {
            var employeeId = _currentUserService.EmployeeId;

            query = query.Where(p => p.EmployeeAssignments.Any(a =>
                a.EmployeeId == employeeId
                && a.StartDate <= today
                && (a.EndDate == null || a.EndDate >= today)));
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ReportableProjectDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);
    }
}
