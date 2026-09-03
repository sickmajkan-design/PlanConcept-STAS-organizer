using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Customers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Customers.Queries.GetCustomers;

public record GetCustomersQuery : ISortablePagedQuery, IRequest<PagedList<CustomerDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "name", "contactPerson", "phone", "projectCount", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches name, contact person and phone (case-insensitive).</summary>
    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetCustomersQueryValidator : SortablePagedQueryValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
        : base(GetCustomersQuery.AllowedSortFields)
    {
    }
}

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedList<CustomerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCustomersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<CustomerDto>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(c =>
                EF.Functions.Like(c.Name.ToLower(), pattern, SearchPattern.Escape) ||
                (c.ContactPerson != null && EF.Functions.Like(c.ContactPerson.ToLower(), pattern, SearchPattern.Escape)) ||
                (c.Phone != null && EF.Functions.Like(c.Phone.ToLower(), pattern, SearchPattern.Escape)));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<CustomerDto>.CreateAsync(
            query.Select(CustomerMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Customer> ApplySorting(
        IQueryable<Customer> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Customer> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("contactperson", false) => query.OrderBy(c => c.ContactPerson),
            ("contactperson", true) => query.OrderByDescending(c => c.ContactPerson),
            ("phone", false) => query.OrderBy(c => c.Phone),
            ("phone", true) => query.OrderByDescending(c => c.Phone),
            ("projectcount", false) => query.OrderBy(c => c.Projects.Count),
            ("projectcount", true) => query.OrderByDescending(c => c.Projects.Count),
            ("createdat", false) => query.OrderBy(c => c.CreatedAt),
            ("createdat", true) => query.OrderByDescending(c => c.CreatedAt),
            (_, true) => query.OrderByDescending(c => c.Name),
            _ => query.OrderBy(c => c.Name)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(c => c.Id);
    }
}
