using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.ArticleOrders.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.ArticleOrders.Queries.GetArticleOrders;

public record GetArticleOrdersQuery : ISortablePagedQuery, IRequest<PagedList<ArticleOrderDto>>
{
    public static readonly string[] AllowedSortFields = ["createdAt", "status"];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public ArticleOrderStatus? Status { get; init; }

    /// <summary>Only the ones not yet in the requester's hands: requested, ordered, on their way.</summary>
    public bool OpenOnly { get; init; }

    /// <summary>Only what the caller asked for themselves.</summary>
    public bool Mine { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;
}

public class GetArticleOrdersQueryValidator : SortablePagedQueryValidator<GetArticleOrdersQuery>
{
    public GetArticleOrdersQueryValidator()
        : base(GetArticleOrdersQuery.AllowedSortFields)
    {
    }
}

public class GetArticleOrdersQueryHandler : IRequestHandler<GetArticleOrdersQuery, PagedList<ArticleOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetArticleOrdersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<ArticleOrderDto>> Handle(
        GetArticleOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var query = _context.ArticleOrders.AsNoTracking();

        if (request.Mine || !ArticleOrderRules.CanManage(_currentUserService.Role))
        {
            // A foreman also sees what is asked for on the sites they run.
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            var sites = request.Mine
                ? null
                : await ForemanScope.OwnProjectIdsAsync(_context, _currentUserService, today, cancellationToken);

            query = sites is null
                ? query.Where(o => o.RequestedByUserId == userId)
                : query.Where(o => o.RequestedByUserId == userId
                    || (o.ProjectId != null && sites.Contains(o.ProjectId.Value)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(o => o.Status == status);
        }

        if (request.OpenOnly)
        {
            query = query.Where(o => o.Status == ArticleOrderStatus.Requested
                || o.Status == ArticleOrderStatus.Ordered
                || o.Status == ArticleOrderStatus.InDelivery);
        }

        IOrderedQueryable<ArticleOrder> ordered = (request.SortBy?.ToLowerInvariant(), request.SortDescending) switch
        {
            ("status", false) => query.OrderBy(o => o.Status),
            ("status", true) => query.OrderByDescending(o => o.Status),
            (_, false) => query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        return await PagedList<ArticleOrderDto>.CreateAsync(
            ordered.ThenBy(o => o.Id).Select(ArticleOrderMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
