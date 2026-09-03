using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Projects.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Queries.GetProjects;

public record GetProjectsQuery : ISortablePagedQuery, IRequest<PagedList<ProjectDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "name", "customerName", "status", "employeeCount", "startDate", "endDate", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches name, customer name and address (case-insensitive).</summary>
    public string? Search { get; init; }

    public ProjectStatus? Status { get; init; }

    public Guid? CustomerId { get; init; }

    /// <summary>Restricts results to the sub-projects of this Main project.</summary>
    public Guid? ParentProjectId { get; init; }

    /// <summary>"Main" for projects with no parent, "Sub" for those with one.</summary>
    public string? Kind { get; init; }

    /// <summary>Restricts results to projects the given employee is assigned to.</summary>
    public Guid? EmployeeId { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetProjectsQueryValidator : SortablePagedQueryValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
        // The admin panel's Projects screen is a customer-grouped board, not
        // a paged grid — it asks for everything in one page-worth, so the
        // cap has to be raised past the default to actually fit that.
        : base(GetProjectsQuery.AllowedSortFields, maxPageSize: 500)
    {
    }
}

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, PagedList<ProjectDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<ProjectDto>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(p =>
                EF.Functions.Like(p.Name.ToLower(), pattern, SearchPattern.Escape) ||
                (p.Customer != null && EF.Functions.Like(p.Customer.Name.ToLower(), pattern, SearchPattern.Escape)) ||
                (p.Address != null && EF.Functions.Like(p.Address.ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(p => p.Status == status);
        }

        if (request.CustomerId is { } customerId)
        {
            query = query.Where(p => p.CustomerId == customerId);
        }

        if (request.ParentProjectId is { } parentProjectId)
        {
            query = query.Where(p => p.ParentProjectId == parentProjectId);
        }

        if (string.Equals(request.Kind, "Main", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.ParentProjectId == null);
        }
        else if (string.Equals(request.Kind, "Sub", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.ParentProjectId != null);
        }

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(p => p.EmployeeAssignments.Any(ea => ea.EmployeeId == employeeId));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<ProjectDto>.CreateAsync(
            query.Select(ProjectMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Project> ApplySorting(
        IQueryable<Project> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Project> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("customername", false) => query.OrderBy(p => p.Customer != null ? p.Customer.Name : null),
            ("customername", true) => query.OrderByDescending(p => p.Customer != null ? p.Customer.Name : null),
            ("status", false) => query.OrderBy(p => p.Status),
            ("status", true) => query.OrderByDescending(p => p.Status),
            ("employeecount", false) => query
                .OrderBy(p => p.EmployeeAssignments.Count(a => a.EndDate == null)),
            ("employeecount", true) => query
                .OrderByDescending(p => p.EmployeeAssignments.Count(a => a.EndDate == null)),
            ("startdate", false) => query.OrderBy(p => p.StartDate),
            ("startdate", true) => query.OrderByDescending(p => p.StartDate),
            ("enddate", false) => query.OrderBy(p => p.EndDate),
            ("enddate", true) => query.OrderByDescending(p => p.EndDate),
            ("createdat", false) => query.OrderBy(p => p.CreatedAt),
            ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
            (_, true) => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(p => p.Id);
    }
}
