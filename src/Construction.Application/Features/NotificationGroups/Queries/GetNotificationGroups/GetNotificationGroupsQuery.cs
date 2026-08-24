using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.NotificationGroups.Models;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Construction.Application.Features.NotificationGroups.Queries.GetNotificationGroups;

public record GetNotificationGroupsQuery : ISortablePagedQuery, IRequest<PagedList<NotificationGroupDto>>
{
    public static readonly string[] AllowedSortFields = ["name", "createdAt"];

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

        query = request.SortDescending
            ? (request.SortBy?.ToLowerInvariant() == "name"
                ? query.OrderByDescending(g => g.Name)
                : query.OrderByDescending(g => g.CreatedAt))
            : (request.SortBy?.ToLowerInvariant() == "name"
                ? query.OrderBy(g => g.Name)
                : query.OrderBy(g => g.CreatedAt));

        return await PagedList<NotificationGroupDto>.CreateAsync(
            query.Select(NotificationGroupMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
