using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.WeeklySiteReports.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetMyWeeklySiteReports;

/// <summary>
/// What a Foreman (or Worker who's filed a report) has submitted themselves —
/// the mobile app's own history, distinct from <c>GetWeeklySiteReportsQuery</c>
/// which is the whole office's inbox and stays Admin-and-above. Scoped to the
/// caller's own employee id the same way <c>GetMyReportableProjectsQuery</c>
/// scopes its own site list, so a foreman sees exactly what they filed and
/// nothing anyone else on the site sent in.
/// </summary>
public record GetMyWeeklySiteReportsQuery : ISortablePagedQuery, IRequest<PagedList<WeeklySiteReportDto>>
{
    public static readonly string[] AllowedSortFields = ["createdAt", "isoWeek"];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;
}

public class GetMyWeeklySiteReportsQueryValidator
    : SortablePagedQueryValidator<GetMyWeeklySiteReportsQuery>
{
    public GetMyWeeklySiteReportsQueryValidator()
        : base(GetMyWeeklySiteReportsQuery.AllowedSortFields)
    {
    }
}

public class GetMyWeeklySiteReportsQueryHandler
    : IRequestHandler<GetMyWeeklySiteReportsQuery, PagedList<WeeklySiteReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyWeeklySiteReportsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<WeeklySiteReportDto>> Handle(
        GetMyWeeklySiteReportsQuery request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId;

        var query = _context.WeeklySiteReports
            .AsNoTracking()
            .Where(r => r.SubmittedByEmployeeId == employeeId);

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "isoweek" => request.SortDescending
                ? query.OrderByDescending(r => r.IsoYear).ThenByDescending(r => r.IsoWeek)
                : query.OrderBy(r => r.IsoYear).ThenBy(r => r.IsoWeek),
            _ => request.SortDescending
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt),
        };

        return await PagedList<WeeklySiteReportDto>.CreateAsync(
            query.Select(WeeklySiteReportMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
