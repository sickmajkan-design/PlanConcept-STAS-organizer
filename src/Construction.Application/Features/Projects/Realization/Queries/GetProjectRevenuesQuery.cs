using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Projects.Realization.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Realization.Queries;

/// <summary>The individual payments the realization plan is built from.</summary>
public record GetProjectRevenuesQuery : ISortablePagedQuery, IRequest<PagedList<ProjectRevenueDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "projectName", "amount", "occurredOn", "note"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ProjectId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetProjectRevenuesQueryValidator : SortablePagedQueryValidator<GetProjectRevenuesQuery>
{
    public GetProjectRevenuesQueryValidator()
        : base(GetProjectRevenuesQuery.AllowedSortFields, maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetProjectRevenuesQueryHandler
    : IRequestHandler<GetProjectRevenuesQuery, PagedList<ProjectRevenueDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectRevenuesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<ProjectRevenueDto>> Handle(
        GetProjectRevenuesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ProjectRevenues.AsNoTracking();

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(r => r.ProjectId == projectId);
        }

        if (request.From is { } from)
        {
            query = query.Where(r => r.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(r => r.OccurredOn <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<ProjectRevenueDto>.CreateAsync(
            query.Select(ProjectRevenueMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<ProjectRevenue> ApplySorting(
        IQueryable<ProjectRevenue> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<ProjectRevenue> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("projectname", false) => query.OrderBy(r => r.Project.Name),
            ("projectname", true) => query.OrderByDescending(r => r.Project.Name),
            ("amount", false) => query.OrderBy(r => r.Amount),
            ("amount", true) => query.OrderByDescending(r => r.Amount),
            ("occurredon", false) => query.OrderBy(r => r.OccurredOn),
            ("note", false) => query.OrderBy(r => r.Note == null).ThenBy(r => r.Note),
            ("note", true) => query.OrderByDescending(r => r.Note == null).ThenByDescending(r => r.Note),
            // Default and explicit "occurredOn desc" both land here: newest
            // payment first, which is what a running ledger reads as.
            _ => query.OrderByDescending(r => r.OccurredOn)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id);
    }
}
