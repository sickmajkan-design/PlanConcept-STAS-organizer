using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Accommodations.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Queries.GetAccommodations;

public record GetAccommodationsQuery : ISortablePagedQuery, IRequest<PagedList<AccommodationDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "address", "name", "city", "type", "beds", "currentOccupants", "currentMonthlyAmount", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches address (case-insensitive).</summary>
    public string? Search { get; init; }

    public AccommodationType? Type { get; init; }

    /// <summary>True: only the ones still rented. False: only the ones given up. Omit for both.</summary>
    public bool? IsActive { get; init; }

    /// <summary>Only rented places whose contract ends within this many days (or already has).</summary>
    public int? ContractEndsWithinDays { get; init; }

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
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccommodationsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
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
                EF.Functions.Like(a.Address.ToLower(), pattern, SearchPattern.Escape)
                || (a.Name != null && EF.Functions.Like(a.Name.ToLower(), pattern, SearchPattern.Escape))
                || (a.City != null && EF.Functions.Like(a.City.ToLower(), pattern, SearchPattern.Escape))
                || (a.LandlordName != null && EF.Functions.Like(a.LandlordName.ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.Type is { } type)
        {
            query = query.Where(a => a.Type == type);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(a => a.IsActive == isActive);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        if (request.ContractEndsWithinDays is { } withinDays)
        {
            var horizon = today.AddDays(withinDays);
            query = query.Where(a => a.IsActive && a.ContractEnd != null && a.ContractEnd <= horizon);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending, today);

        var page = await PagedList<AccommodationDto>.CreateAsync(
            query.Select(AccommodationMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        await AccommodationOccupancy.FillCurrentOccupantsAsync(_context, page.Items, today, cancellationToken);

        return page;
    }

    private static IQueryable<Accommodation> ApplySorting(
        IQueryable<Accommodation> query,
        string? sortBy,
        bool descending,
        DateOnly today)
    {
        IOrderedQueryable<Accommodation> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("name", false) => query.OrderBy(a => a.Name == null).ThenBy(a => a.Name),
            ("name", true) => query.OrderByDescending(a => a.Name == null).ThenByDescending(a => a.Name),
            ("city", false) => query.OrderBy(a => a.City == null).ThenBy(a => a.City),
            ("city", true) => query.OrderByDescending(a => a.City == null).ThenByDescending(a => a.City),
            ("type", false) => query.OrderBy(a => a.Type),
            ("type", true) => query.OrderByDescending(a => a.Type),
            ("beds", false) => query.OrderBy(a => a.Beds == null).ThenBy(a => a.Beds),
            ("beds", true) => query.OrderByDescending(a => a.Beds == null).ThenByDescending(a => a.Beds),
            ("currentoccupants", false) => query.OrderBy(a => a.Stays.Count(s => s.StartDate <= today && (s.EndDate == null || s.EndDate >= today))),
            ("currentoccupants", true) => query.OrderByDescending(a => a.Stays.Count(s => s.StartDate <= today && (s.EndDate == null || s.EndDate >= today))),
            ("currentmonthlyamount", false) => query
                .OrderBy(a => a.Rates.Where(r => r.EndDate == null && r.Kind == AccommodationChargeKind.Monthly).Select(r => (decimal?)r.Amount).FirstOrDefault()),
            ("currentmonthlyamount", true) => query
                .OrderByDescending(a => a.Rates.Where(r => r.EndDate == null && r.Kind == AccommodationChargeKind.Monthly).Select(r => (decimal?)r.Amount).FirstOrDefault()),
            ("createdat", false) => query.OrderBy(a => a.CreatedAt),
            ("createdat", true) => query.OrderByDescending(a => a.CreatedAt),
            (_, true) => query.OrderByDescending(a => a.Address),
            _ => query.OrderBy(a => a.Address)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(a => a.Id);
    }
}
