using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
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
        "name", "client", "status", "startDate", "endDate", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches name, client and address (case-insensitive).</summary>
    public string? Search { get; init; }

    public ProjectStatus? Status { get; init; }

    public string? Client { get; init; }

    /// <summary>Restricts results to projects the given employee is assigned to.</summary>
    public Guid? EmployeeId { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetProjectsQueryValidator : SortablePagedQueryValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
        : base(GetProjectsQuery.AllowedSortFields)
    {
    }
}

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, PagedList<ProjectDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProjectsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedList<ProjectDto>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim().ToLowerInvariant()}%";

            query = query.Where(p =>
                EF.Functions.Like(p.Name.ToLower(), pattern) ||
                (p.Client != null && EF.Functions.Like(p.Client.ToLower(), pattern)) ||
                (p.Address != null && EF.Functions.Like(p.Address.ToLower(), pattern)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Client))
        {
            query = query.Where(p => p.Client != null && EF.Functions.Like(
                p.Client.ToLower(), $"%{request.Client.Trim().ToLowerInvariant()}%"));
        }

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(p => p.EmployeeAssignments.Any(ea => ea.EmployeeId == employeeId));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<ProjectDto>.CreateAsync(
            query.ProjectTo<ProjectDto>(_mapper.ConfigurationProvider),
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
            ("client", false) => query.OrderBy(p => p.Client),
            ("client", true) => query.OrderByDescending(p => p.Client),
            ("status", false) => query.OrderBy(p => p.Status),
            ("status", true) => query.OrderByDescending(p => p.Status),
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
