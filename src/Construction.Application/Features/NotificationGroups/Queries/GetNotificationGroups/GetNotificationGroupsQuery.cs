using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.NotificationGroups.Models;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Construction.Application.Features.NotificationGroups.Queries.GetNotificationGroups;

public record GetNotificationGroupsQuery : ISortablePagedQuery, IRequest<PagedList<NotificationGroupDto>>
{
    public static readonly string[] AllowedSortFields = ["name", "memberCount", "createdAt"];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetNotificationGroupsQueryValidator : SortablePagedQueryValidator<GetNotificationGroupsQuery>
{
    public GetNotificationGroupsQueryValidator()
        : base(GetNotificationGroupsQuery.AllowedSortFields)
    {
    }
}

public class GetNotificationGroupsQueryHandler
    : IRequestHandler<GetNotificationGroupsQuery, PagedList<NotificationGroupDto>>
{
    private readonly IApplicationDbContext _context;

    public GetNotificationGroupsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<NotificationGroupDto>> Handle(
        GetNotificationGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.NotificationGroups.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(g =>
                EF.Functions.Like(g.Name.ToLower(), pattern, SearchPattern.Escape));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<NotificationGroupDto>.CreateAsync(
            query.Select(NotificationGroupMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<NotificationGroup> ApplySorting(
        IQueryable<NotificationGroup> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<NotificationGroup> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("name", true) => query.OrderByDescending(g => g.Name),
            ("membercount", false) => query.OrderBy(g => g.Members.Count),
            ("membercount", true) => query.OrderByDescending(g => g.Members.Count),
            ("createdat", false) => query.OrderBy(g => g.CreatedAt),
            ("createdat", true) => query.OrderByDescending(g => g.CreatedAt),
            (_, true) => query.OrderByDescending(g => g.CreatedAt),
            _ => query.OrderBy(g => g.Name)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(g => g.Id);
    }
}
