using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Queries.GetAbsences;

public record GetAbsencesQuery : ISortablePagedQuery, IRequest<PagedList<AbsenceDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "startDate", "endDate", "employeeName", "status", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? EmployeeId { get; init; }

    public AbsenceStatus? Status { get; init; }

    public AbsenceType? Type { get; init; }

    /// <summary>Absences overlapping this window, not only ones inside it.</summary>
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;
}

public class GetAbsencesQueryValidator : SortablePagedQueryValidator<GetAbsencesQuery>
{
    public GetAbsencesQueryValidator()
        : base(GetAbsencesQuery.AllowedSortFields)
    {
    }
}

public class GetAbsencesQueryHandler : IRequestHandler<GetAbsencesQuery, PagedList<AbsenceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAbsencesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<AbsenceDto>> Handle(
        GetAbsencesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Absences.AsNoTracking();

        // A worker sees their own leave and nobody else's. Filtered rather than
        // refused, so asking for someone else's id returns their own.
        if (AbsenceRules.IsRestrictedToOwnAbsences(_currentUserService.Role))
        {
            var ownEmployeeId = _currentUserService.EmployeeId;

            if (ownEmployeeId is null)
            {
                return new PagedList<AbsenceDto>(
                    Array.Empty<AbsenceDto>(), 0, request.PageNumber, request.PageSize);
            }

            query = query.Where(a => a.EmployeeId == ownEmployeeId);
        }
        else if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(a => a.EmployeeId == employeeId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (request.Type is { } type)
        {
            query = query.Where(a => a.Type == type);
        }

        if (request.From is { } from)
        {
            query = query.Where(a => a.EndDate >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(a => a.StartDate <= to);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<AbsenceDto>.CreateAsync(
            query.Select(AbsenceMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Absence> ApplySorting(
        IQueryable<Absence> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Absence> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("enddate", false) => query.OrderBy(a => a.EndDate),
            ("enddate", true) => query.OrderByDescending(a => a.EndDate),
            ("employeename", false) => query
                .OrderBy(a => a.Employee.LastName).ThenBy(a => a.Employee.FirstName),
            ("employeename", true) => query
                .OrderByDescending(a => a.Employee.LastName)
                .ThenByDescending(a => a.Employee.FirstName),
            ("status", false) => query.OrderBy(a => a.Status),
            ("status", true) => query.OrderByDescending(a => a.Status),
            ("createdat", false) => query.OrderBy(a => a.CreatedAt),
            ("createdat", true) => query.OrderByDescending(a => a.CreatedAt),
            (_, false) => query.OrderBy(a => a.StartDate),
            _ => query.OrderByDescending(a => a.StartDate)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(a => a.Id);
    }
}
