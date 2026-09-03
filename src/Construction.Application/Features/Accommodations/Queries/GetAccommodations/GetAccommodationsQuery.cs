using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Accommodations.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Queries.GetAccommodations;

public record GetAccommodationsQuery : ISortablePagedQuery, IRequest<PagedList<AccommodationDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "address", "currentMonthlyAmount", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches address (case-insensitive).</summary>
    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetAccommodationsQueryValidator : SortablePagedQueryValidator<GetAccommodationsQuery>
{
    public GetAccommodationsQueryValidator()
        : base(GetAccommodationsQuery.AllowedSortFields)
    {
    }
}

public class GetAccommodationsQueryHandler
    : IRequestHandler<GetAccommodationsQuery, PagedList<AccommodationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAccommodationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<AccommodationDto>> Handle(
        GetAccommodationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Accommodations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(a =>
                EF.Functions.Like(a.Address.ToLower(), pattern, SearchPattern.Escape));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<AccommodationDto>.CreateAsync(
            query.Select(AccommodationMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Accommodation> ApplySorting(
        IQueryable<Accommodation> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Accommodation> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("currentmonthlyamount", false) => query
                .OrderBy(a => a.Rates.Where(r => r.EndDate == null).Select(r => (decimal?)r.MonthlyAmount).FirstOrDefault()),
            ("currentmonthlyamount", true) => query
                .OrderByDescending(a => a.Rates.Where(r => r.EndDate == null).Select(r => (decimal?)r.MonthlyAmount).FirstOrDefault()),
            ("createdat", false) => query.OrderBy(a => a.CreatedAt),
            ("createdat", true) => query.OrderByDescending(a => a.CreatedAt),
            (_, true) => query.OrderByDescending(a => a.Address),
            _ => query.OrderBy(a => a.Address)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(a => a.Id);
    }
}
