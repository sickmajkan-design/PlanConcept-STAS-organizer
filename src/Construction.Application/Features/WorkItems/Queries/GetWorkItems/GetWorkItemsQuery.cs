using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.WorkItems.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems.Queries.GetWorkItems;

public record GetWorkItemsQuery : ISortablePagedQuery, IRequest<PagedList<WorkItemDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "title", "dueDate", "priority", "status", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches the title and description (case-insensitive).</summary>
    public string? Search { get; init; }

    public WorkItemKind? Kind { get; init; }

    public WorkItemStatus? Status { get; init; }

    public WorkItemPriority? Priority { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    /// <summary>Only what is still to do — the default view of a board.</summary>
    public bool? OpenOnly { get; init; }

    /// <summary>Only what is past its deadline and not finished.</summary>
    public bool? OverdueOnly { get; init; }

    /// <summary>Only work with nobody on it.</summary>
    public bool? UnassignedOnly { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetWorkItemsQueryValidator : SortablePagedQueryValidator<GetWorkItemsQuery>
{
    public GetWorkItemsQueryValidator()
        : base(GetWorkItemsQuery.AllowedSortFields)
    {
    }
}

public class GetWorkItemsQueryHandler
    : IRequestHandler<GetWorkItemsQuery, PagedList<WorkItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public GetWorkItemsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<PagedList<WorkItemDto>> Handle(
        GetWorkItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.WorkItems.AsNoTracking();

        // A Worker sees their own list and nobody else's. Filtered here rather
        // than refused at the route, so asking for someone else's id returns
        // their own work instead of a 403 confirming the id exists.
        if (WorkItemRules.IsRestrictedToOwnItems(_currentUserService.Role))
        {
            var ownEmployeeId = _currentUserService.EmployeeId;

            if (ownEmployeeId is null)
            {
                return new PagedList<WorkItemDto>(
                    Array.Empty<WorkItemDto>(), 0, request.PageNumber, request.PageSize);
            }

            query = query.Where(w => w.AssignedEmployeeId == ownEmployeeId);
        }
        else if (request.AssignedEmployeeId is { } employeeId)
        {
            query = query.Where(w => w.AssignedEmployeeId == employeeId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(w =>
                EF.Functions.Like(w.Title.ToLower(), pattern) ||
                (w.Description != null &&
                 EF.Functions.Like(w.Description.ToLower(), pattern)));
        }

        if (request.Kind is { } kind)
        {
            query = query.Where(w => w.Kind == kind);
        }

        if (request.Status is { } status)
        {
            query = query.Where(w => w.Status == status);
        }

        if (request.Priority is { } priority)
        {
            query = query.Where(w => w.Priority == priority);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(w => w.ProjectId == projectId);
        }

        if (request.UnassignedOnly == true)
        {
            query = query.Where(w => w.AssignedEmployeeId == null);
        }

        if (request.OpenOnly == true)
        {
            query = query.Where(w =>
                w.Status != WorkItemStatus.Closed && w.Status != WorkItemStatus.Cancelled);
        }

        if (request.OverdueOnly == true)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

            query = query.Where(w =>
                w.DueDate != null
                && w.DueDate < today
                && w.Status != WorkItemStatus.Closed
                && w.Status != WorkItemStatus.Cancelled);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<WorkItemDto>.CreateAsync(
            query.ProjectTo<WorkItemDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<WorkItem> ApplySorting(
        IQueryable<WorkItem> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<WorkItem> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("title", false) => query.OrderBy(w => w.Title),
            ("title", true) => query.OrderByDescending(w => w.Title),
            ("priority", false) => query.OrderBy(w => w.Priority),
            ("priority", true) => query.OrderByDescending(w => w.Priority),
            ("status", false) => query.OrderBy(w => w.Status),
            ("status", true) => query.OrderByDescending(w => w.Status),
            ("createdat", false) => query.OrderBy(w => w.CreatedAt),
            ("createdat", true) => query.OrderByDescending(w => w.CreatedAt),
            ("duedate", true) => query.OrderByDescending(w => w.DueDate),
            // Default and explicit "dueDate" both land here: soonest first,
            // with undated work after it rather than sorted to the top as
            // PostgreSQL would put nulls by default on an ascending sort.
            _ => query.OrderBy(w => w.DueDate == null).ThenBy(w => w.DueDate)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(w => w.Id);
    }
}
