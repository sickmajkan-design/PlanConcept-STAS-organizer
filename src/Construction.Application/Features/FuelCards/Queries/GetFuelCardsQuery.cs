using Construction.Application.Common.Security;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Costs;
using Construction.Application.Features.FuelCards.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelCards.Queries;

public record GetFuelCardsQuery : ISortablePagedQuery, IRequest<PagedList<FuelCardDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "provider", "cardNumber", "issuedOn", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? VehicleId { get; init; }

    /// <summary>Matches the card number, the provider, or the vehicle it is on (name, plate or TD number).</summary>
    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetFuelCardsQueryValidator : SortablePagedQueryValidator<GetFuelCardsQuery>
{
    public GetFuelCardsQueryValidator()
        : base(GetFuelCardsQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetFuelCardsQueryHandler : IRequestHandler<GetFuelCardsQuery, PagedList<FuelCardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFuelCardsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<FuelCardDto>> Handle(
        GetFuelCardsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see fuel cards.");
        }

        var query = _context.FuelCards.AsNoTracking();

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(c => c.VehicleId == vehicleId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(c =>
                EF.Functions.Like(c.CardNumber.ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like(c.Provider.ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like((c.Vehicle.Brand + " " + c.Vehicle.Model).ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like(c.Vehicle.RegistrationNumber.ToLower(), pattern, SearchPattern.Escape) ||
                (c.Vehicle.TdNumber != null && EF.Functions.Like(c.Vehicle.TdNumber.ToLower(), pattern, SearchPattern.Escape)));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<FuelCardDto>.CreateAsync(
            query.Select(FuelCardMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<FuelCard> ApplySorting(
        IQueryable<FuelCard> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<FuelCard> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("provider", false) => query.OrderBy(c => c.Provider),
            ("provider", true) => query.OrderByDescending(c => c.Provider),
            ("cardnumber", false) => query.OrderBy(c => c.CardNumber),
            ("cardnumber", true) => query.OrderByDescending(c => c.CardNumber),
            ("issuedon", false) => query.OrderBy(c => c.IssuedOn == null).ThenBy(c => c.IssuedOn),
            ("issuedon", true) => query
                .OrderByDescending(c => c.IssuedOn == null).ThenByDescending(c => c.IssuedOn),
            ("createdat", false) => query.OrderBy(c => c.CreatedAt),
            ("createdat", true) => query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(c => c.Id);
    }
}
