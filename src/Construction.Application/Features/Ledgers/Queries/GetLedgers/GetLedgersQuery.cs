using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgers;

public record GetLedgersQuery : ISortablePagedQuery, IRequest<PagedList<LedgerSummaryDto>>
{
    public static readonly string[] AllowedSortFields = ["name", "year", "month", "createdAt"];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches name (case-insensitive).</summary>
    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetLedgersQueryValidator : SortablePagedQueryValidator<GetLedgersQuery>
{
    public GetLedgersQueryValidator()
        : base(GetLedgersQuery.AllowedSortFields)
    {
    }
}

public class GetLedgersQueryHandler : IRequestHandler<GetLedgersQuery, PagedList<LedgerSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLedgersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<LedgerSummaryDto>> Handle(
        GetLedgersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Ledgers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(l =>
                EF.Functions.Like(l.Name.ToLower(), pattern, SearchPattern.Escape));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<LedgerSummaryDto>.CreateAsync(
            query.Select(LedgerSummaryMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Ledger> ApplySorting(
        IQueryable<Ledger> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Ledger> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("name", false) => query.OrderBy(l => l.Name),
            ("name", true) => query.OrderByDescending(l => l.Name),
            ("year", false) => query.OrderBy(l => l.Year).ThenBy(l => l.Month),
            ("month", false) => query.OrderBy(l => l.Year).ThenBy(l => l.Month),
            ("createdat", false) => query.OrderBy(l => l.CreatedAt),
            ("createdat", true) => query.OrderByDescending(l => l.CreatedAt),
            // Default and "year/month desc" both land here: most recent month
            // first, which is what opening the list wants to see.
            _ => query.OrderByDescending(l => l.Year).ThenByDescending(l => l.Month)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(l => l.Id);
    }
}
