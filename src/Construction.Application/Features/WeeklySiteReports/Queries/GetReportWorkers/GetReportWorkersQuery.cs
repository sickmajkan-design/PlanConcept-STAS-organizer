using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetReportWorkers;

public record ReportWorkerDto(Guid Id, string FullName, string? Position);

/// <summary>
/// The people a foreman can put in a weekly report. Without a search these are the crew of his own sites; with at
/// least two letters of a name they are anyone who works for the firm, because he can have somebody with him who was
/// never entered in the planner. Only names and positions are returned, so this is not a way to read the employee
/// records a foreman does not otherwise see.
/// </summary>
public record GetReportWorkersQuery(string? Search) : IRequest<IReadOnlyList<ReportWorkerDto>>;

public class GetReportWorkersQueryHandler : IRequestHandler<GetReportWorkersQuery, IReadOnlyList<ReportWorkerDto>>
{
    private const int MinSearchLength = 2;
    private const int Shown = 40;
    private const int RecentDays = 14;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetReportWorkersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<ReportWorkerDto>> Handle(
        GetReportWorkersQuery request, CancellationToken cancellationToken)
    {
        var people = _context.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active);

        var search = request.Search?.Trim();

        if (!string.IsNullOrEmpty(search))
        {
            if (search.Length < MinSearchLength)
            {
                return [];
            }

            var pattern = SearchPattern.Contains(search);

            people = people.Where(e =>
                EF.Functions.Like((e.FirstName + " " + e.LastName).ToLower(), pattern, SearchPattern.Escape)
                || EF.Functions.Like((e.LastName + " " + e.FirstName).ToLower(), pattern, SearchPattern.Escape));
        }
        else
        {
            // The crew of the caller's own sites, today and in the last weeks, himself included.
            var since = _dateTimeProvider.Today.AddDays(-RecentDays);
            var myEmployeeId = _currentUserService.EmployeeId;

            var mySites = _context.EmployeeProjects
                .Where(a => a.EmployeeId == myEmployeeId && (a.EndDate == null || a.EndDate >= since))
                .Select(a => a.ProjectId);

            var crew = _context.EmployeeProjects
                .Where(a => mySites.Contains(a.ProjectId) && (a.EndDate == null || a.EndDate >= since))
                .Select(a => a.EmployeeId);

            people = people.Where(e => crew.Contains(e.Id));
        }

        return await people
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Take(Shown)
            .Select(e => new ReportWorkerDto(e.Id, e.FirstName + " " + e.LastName, e.Position))
            .ToListAsync(cancellationToken);
    }
}
