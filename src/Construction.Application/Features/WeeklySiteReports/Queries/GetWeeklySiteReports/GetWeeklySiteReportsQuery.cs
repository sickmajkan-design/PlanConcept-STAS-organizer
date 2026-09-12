using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.WeeklySiteReports.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReports;

/// <summary>
/// The office's inbox of submitted weekly reports — filterable by site and
/// ISO week so it reads as "sorted by site and KW" the moment it opens,
/// exactly what a foreman's submission is supposed to land as.
/// </summary>
public record GetWeeklySiteReportsQuery : ISortablePagedQuery, IRequest<PagedList<WeeklySiteReportDto>>
{
    public static readonly string[] AllowedSortFields = ["createdAt", "isoWeek"];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ProjectId { get; init; }

    public int? IsoYear { get; init; }

    public int? IsoWeek { get; init; }

    public WeeklyReportStatus? Status { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;
}

public class GetWeeklySiteReportsQueryValidator : SortablePagedQueryValidator<GetWeeklySiteReportsQuery>
{
    public GetWeeklySiteReportsQueryValidator()
        : base(GetWeeklySiteReportsQuery.AllowedSortFields)
    {
    }
}

public class GetWeeklySiteReportsQueryHandler
    : IRequestHandler<GetWeeklySiteReportsQuery, PagedList<WeeklySiteReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetWeeklySiteReportsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<WeeklySiteReportDto>> Handle(
        GetWeeklySiteReportsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.WeeklySiteReports.AsNoTracking();

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(r => r.ProjectId == projectId);
        }

        if (request.IsoYear is { } isoYear)
        {
            query = query.Where(r => r.IsoYear == isoYear);
        }

        if (request.IsoWeek is { } isoWeek)
        {
            query = query.Where(r => r.IsoWeek == isoWeek);
        }

        if (request.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

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
