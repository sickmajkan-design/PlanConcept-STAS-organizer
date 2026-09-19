using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Stays;

/// <summary>Who lives (or lived) where, newest first.</summary>
public record GetAccommodationStaysQuery : ISortablePagedQuery, IRequest<PagedList<AccommodationStayDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "startDate", "endDate", "employeeName", "accommodationName", "projectName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? AccommodationId { get; init; }

    public Guid? EmployeeId { get; init; }

    /// <summary>Only the stays that cover today.</summary>
    public bool CurrentOnly { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetAccommodationStaysQueryValidator : SortablePagedQueryValidator<GetAccommodationStaysQuery>
{
    public GetAccommodationStaysQueryValidator()
        : base(GetAccommodationStaysQuery.AllowedSortFields, maxPageSize: 200)
    {
    }
}

public class GetAccommodationStaysQueryHandler
    : IRequestHandler<GetAccommodationStaysQuery, PagedList<AccommodationStayDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccommodationStaysQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<AccommodationStayDto>> Handle(
        GetAccommodationStaysQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AccommodationStays.AsNoTracking();

        if (request.AccommodationId is { } accommodationId)
        {
            query = query.Where(s => s.AccommodationId == accommodationId);
        }

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(s => s.EmployeeId == employeeId);
        }

        if (request.CurrentOnly)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            query = query.Where(s => s.StartDate <= today && (s.EndDate == null || s.EndDate >= today));
        }

        IOrderedQueryable<Domain.Entities.AccommodationStay> ordered =
            (request.SortBy?.ToLowerInvariant(), request.SortDescending) switch
            {
                ("enddate", false) => query.OrderBy(s => s.EndDate == null).ThenBy(s => s.EndDate),
                ("enddate", true) => query.OrderByDescending(s => s.EndDate == null).ThenByDescending(s => s.EndDate),
                ("employeename", false) => query.OrderBy(s => s.Employee.LastName).ThenBy(s => s.Employee.FirstName),
                ("employeename", true) => query.OrderByDescending(s => s.Employee.LastName).ThenByDescending(s => s.Employee.FirstName),
                ("accommodationname", false) => query.OrderBy(s => s.Accommodation.Name ?? s.Accommodation.Address),
                ("accommodationname", true) => query.OrderByDescending(s => s.Accommodation.Name ?? s.Accommodation.Address),
                ("projectname", false) => query.OrderBy(s => s.Project == null).ThenBy(s => s.Project!.Name),
                ("projectname", true) => query.OrderByDescending(s => s.Project == null).ThenByDescending(s => s.Project!.Name),
                ("createdat", false) => query.OrderBy(s => s.CreatedAt),
                ("createdat", true) => query.OrderByDescending(s => s.CreatedAt),
                ("startdate", false) => query.OrderBy(s => s.StartDate),
                _ => query.OrderByDescending(s => s.StartDate)
            };

        return await PagedList<AccommodationStayDto>.CreateAsync(
            ordered.ThenBy(s => s.Id).Select(AccommodationStayMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
