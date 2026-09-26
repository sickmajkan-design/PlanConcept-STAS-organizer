using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Refunds.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Refunds.Queries.GetRefunds;

public record GetRefundsQuery : ISortablePagedQuery, IRequest<PagedList<RefundDto>>
{
    public static readonly string[] AllowedSortFields = ["createdAt", "expenseDate", "amount", "status"];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public RefundStatus? Status { get; init; }

    /// <summary>Only what the caller asked for themselves.</summary>
    public bool Mine { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;
}

public class GetRefundsQueryValidator : SortablePagedQueryValidator<GetRefundsQuery>
{
    public GetRefundsQueryValidator()
        : base(GetRefundsQuery.AllowedSortFields)
    {
    }
}

public class GetRefundsQueryHandler : IRequestHandler<GetRefundsQuery, PagedList<RefundDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRefundsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<RefundDto>> Handle(GetRefundsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var query = _context.Refunds.AsNoTracking();

        // Money: the office sees everyone's, everybody else only what they asked for themselves.
        if (request.Mine || !RefundRules.CanReview(_currentUserService.Role))
        {
            query = query.Where(r => r.RequestedByUserId == userId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        IOrderedQueryable<Refund> ordered = (request.SortBy?.ToLowerInvariant(), request.SortDescending) switch
        {
            ("expensedate", false) => query.OrderBy(r => r.ExpenseDate),
            ("expensedate", true) => query.OrderByDescending(r => r.ExpenseDate),
            ("amount", false) => query.OrderBy(r => r.Amount),
            ("amount", true) => query.OrderByDescending(r => r.Amount),
            ("status", false) => query.OrderBy(r => r.Status),
            ("status", true) => query.OrderByDescending(r => r.Status),
            (_, false) => query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        return await PagedList<RefundDto>.CreateAsync(
            ordered.ThenBy(r => r.Id).Select(RefundMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
