using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Materials.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Queries.GetMaterials;

public record GetMaterialsQuery : ISortablePagedQuery, IRequest<PagedList<MaterialDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "name", "unit", "quantity", "warehouse", "lastUpdated", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches name and warehouse (case-insensitive).</summary>
    public string? Search { get; init; }

    public Guid? ProjectId { get; init; }

    public string? Warehouse { get; init; }

    /// <summary>When true, returns only warehouse stock (materials not tied to a project).</summary>
    public bool? UnassignedOnly { get; init; }

    /// <summary>When set, returns only materials whose quantity is at or below this value.</summary>
    public decimal? MaxQuantity { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetMaterialsQueryValidator : SortablePagedQueryValidator<GetMaterialsQuery>
{
    public GetMaterialsQueryValidator()
        : base(GetMaterialsQuery.AllowedSortFields)
    {
        RuleFor(x => x.MaxQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("MaxQuantity must not be negative.")
            .When(x => x.MaxQuantity is not null);
    }
}

public class GetMaterialsQueryHandler : IRequestHandler<GetMaterialsQuery, PagedList<MaterialDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMaterialsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<MaterialDto>> Handle(
        GetMaterialsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(m =>
                EF.Functions.Like(m.Name.ToLower(), pattern, SearchPattern.Escape) ||
                (m.Warehouse != null && EF.Functions.Like(m.Warehouse.ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(m => m.ProjectId == projectId);
        }

        if (!string.IsNullOrWhiteSpace(request.Warehouse))
        {
            var warehousePattern = SearchPattern.Contains(request.Warehouse);

            query = query.Where(m => m.Warehouse != null && EF.Functions.Like(
                m.Warehouse.ToLower(), warehousePattern, SearchPattern.Escape));
        }

        if (request.UnassignedOnly == true)
        {
            query = query.Where(m => m.ProjectId == null);
        }

        if (request.MaxQuantity is { } maxQuantity)
        {
            query = query.Where(m => m.Quantity <= maxQuantity);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<MaterialDto>.CreateAsync(
            query.Select(MaterialMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Material> ApplySorting(
        IQueryable<Material> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Material> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("unit", false) => query.OrderBy(m => m.Unit),
            ("unit", true) => query.OrderByDescending(m => m.Unit),
            ("quantity", false) => query.OrderBy(m => m.Quantity),
            ("quantity", true) => query.OrderByDescending(m => m.Quantity),
            ("warehouse", false) => query.OrderBy(m => m.Warehouse),
            ("warehouse", true) => query.OrderByDescending(m => m.Warehouse),
            ("lastupdated", false) => query.OrderBy(m => m.LastUpdated),
            ("lastupdated", true) => query.OrderByDescending(m => m.LastUpdated),
            ("createdat", false) => query.OrderBy(m => m.CreatedAt),
            ("createdat", true) => query.OrderByDescending(m => m.CreatedAt),
            (_, true) => query.OrderByDescending(m => m.Name),
            _ => query.OrderBy(m => m.Name)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(m => m.Id);
    }
}
