using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Tools.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Queries.GetTools;

public record GetToolsQuery : ISortablePagedQuery, IRequest<PagedList<ToolDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "name", "category", "serialNumber", "status", "assignedEmployeeName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches name, category, serial number and QR code (case-insensitive).</summary>
    public string? Search { get; init; }

    public ToolStatus? Status { get; init; }

    public string? Category { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    public Guid? AssignedProjectId { get; init; }

    /// <summary>When true, returns only tools with no employee and no project assignment.</summary>
    public bool? Unassigned { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetToolsQueryValidator : SortablePagedQueryValidator<GetToolsQuery>
{
    public GetToolsQueryValidator()
        : base(GetToolsQuery.AllowedSortFields)
    {
    }
}

public class GetToolsQueryHandler : IRequestHandler<GetToolsQuery, PagedList<ToolDto>>
{
    private readonly IApplicationDbContext _context;

    public GetToolsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<ToolDto>> Handle(
        GetToolsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Tools.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(t =>
                EF.Functions.Like(t.Name.ToLower(), pattern, SearchPattern.Escape) ||
                (t.Category != null && EF.Functions.Like(t.Category.ToLower(), pattern, SearchPattern.Escape)) ||
                (t.SerialNumber != null && EF.Functions.Like(t.SerialNumber.ToLower(), pattern, SearchPattern.Escape)) ||
                (t.QrCode != null && EF.Functions.Like(t.QrCode.ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var categoryPattern = SearchPattern.Contains(request.Category);

            query = query.Where(t => t.Category != null && EF.Functions.Like(
                t.Category.ToLower(), categoryPattern, SearchPattern.Escape));
        }

        if (request.AssignedEmployeeId is { } employeeId)
        {
            query = query.Where(t => t.AssignedEmployeeId == employeeId);
        }

        if (request.AssignedProjectId is { } projectId)
        {
            query = query.Where(t => t.AssignedProjectId == projectId);
        }

        if (request.Unassigned == true)
        {
            query = query.Where(t => t.AssignedEmployeeId == null && t.AssignedProjectId == null);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<ToolDto>.CreateAsync(
            query.Select(ToolMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Tool> ApplySorting(
        IQueryable<Tool> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Tool> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("category", false) => query.OrderBy(t => t.Category),
            ("category", true) => query.OrderByDescending(t => t.Category),
            ("serialnumber", false) => query.OrderBy(t => t.SerialNumber),
            ("serialnumber", true) => query.OrderByDescending(t => t.SerialNumber),
            ("status", false) => query.OrderBy(t => t.Status),
            ("status", true) => query.OrderByDescending(t => t.Status),
            ("assignedemployeename", false) => query
                .OrderBy(t => t.AssignedEmployee != null ? t.AssignedEmployee.LastName : null)
                .ThenBy(t => t.AssignedEmployee != null ? t.AssignedEmployee.FirstName : null),
            ("assignedemployeename", true) => query
                .OrderByDescending(t => t.AssignedEmployee != null ? t.AssignedEmployee.LastName : null)
                .ThenByDescending(t => t.AssignedEmployee != null ? t.AssignedEmployee.FirstName : null),
            ("createdat", false) => query.OrderBy(t => t.CreatedAt),
            ("createdat", true) => query.OrderByDescending(t => t.CreatedAt),
            (_, true) => query.OrderByDescending(t => t.Name),
            _ => query.OrderBy(t => t.Name)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(t => t.Id);
    }
}
