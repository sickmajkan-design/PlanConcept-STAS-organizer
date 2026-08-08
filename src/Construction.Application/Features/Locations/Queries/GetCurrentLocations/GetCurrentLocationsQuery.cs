using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Locations.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Locations.Queries.GetCurrentLocations;

/// <summary>
/// Latest known position of every employee, for the live admin map.
/// Optionally limited to one project's crew and to recent pings only.
/// </summary>
/// <remarks>
/// <para>
/// Paged, like every other list, though a map is not an obvious thing to page.
/// It used to return every active employee who had ever reported, with no
/// bound at all — fine for the stated scale of a few hundred people, and a
/// response that grows without limit for anybody larger. A page is the only
/// thing that makes the cost of this endpoint knowable in advance.
/// </para>
/// <para>
/// The page is deliberately much larger than the twenty a grid uses, because
/// the caller is drawing markers rather than rows and a map that needs four
/// round trips to fill in is a worse map. The ceiling is a thousand.
/// </para>
/// <para>
/// The envelope carries <c>TotalCount</c>, which matters more here than on a
/// grid: a truncated map has no scrollbar to hint that somebody is missing, so
/// the client compares the two numbers and says so rather than quietly drawing
/// a partial picture.
/// </para>
/// </remarks>
public record GetCurrentLocationsQuery : IPagedQuery, IRequest<PagedList<EmployeeLocationDto>>
{
    /// <summary>The largest page this endpoint will serve.</summary>
    public const int MaxPageSize = 1_000;

    /// <summary>Only employees assigned to this project.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Ignore pings older than this many minutes (default: no limit).</summary>
    public int? MaxAgeMinutes { get; init; }

    /// <summary>Include employees who are not in Active status (default false).</summary>
    public bool IncludeInactive { get; init; }

    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Big enough that a normal deployment draws its whole map in one request.
    /// </summary>
    public int PageSize { get; init; } = 250;
}

public class GetCurrentLocationsQueryValidator : PagedQueryValidator<GetCurrentLocationsQuery>
{
    public GetCurrentLocationsQueryValidator()
        : base(GetCurrentLocationsQuery.MaxPageSize)
    {
        RuleFor(x => x.MaxAgeMinutes)
            .GreaterThanOrEqualTo(1).WithMessage("MaxAgeMinutes must be at least 1.")
            .When(x => x.MaxAgeMinutes is not null);
    }
}

public class GetCurrentLocationsQueryHandler
    : IRequestHandler<GetCurrentLocationsQuery, PagedList<EmployeeLocationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCurrentLocationsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<EmployeeLocationDto>> Handle(
        GetCurrentLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var employees = _context.Employees.AsNoTracking();

        if (!request.IncludeInactive)
        {
            employees = employees.Where(e => e.Status == EmployeeStatus.Active);
        }

        if (request.ProjectId is { } projectId)
        {
            employees = employees.Where(e =>
                e.ProjectAssignments.Any(pa => pa.ProjectId == projectId));
        }

        var cutoff = request.MaxAgeMinutes is { } maxAge
            ? _dateTimeProvider.UtcNow.AddMinutes(-maxAge)
            : (DateTime?)null;

        // One lateral-join query: newest ping per employee, filtered server-side.
        var locations = employees
            .Select(e => new
            {
                Employee = e,
                Last = e.LocationRecords
                    .Where(l => cutoff == null || l.Timestamp >= cutoff)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefault()
            })
            .Where(x => x.Last != null)
            .Select(x => new EmployeeLocationDto
            {
                EmployeeId = x.Employee.Id,
                EmployeeNumber = x.Employee.EmployeeNumber,
                FullName = x.Employee.FirstName + " " + x.Employee.LastName,
                Position = x.Employee.Position,
                Latitude = x.Last!.Latitude,
                Longitude = x.Last.Longitude,
                Accuracy = x.Last.Accuracy,
                Timestamp = x.Last.Timestamp
            });

        return await PagedList<EmployeeLocationDto>.CreateAsync(
            locations
                .OrderBy(l => l.FullName)
                // Namesakes sort equally, so without a unique last key the
                // order between them is undefined between one query and the
                // next, and a second page can repeat somebody while dropping
                // somebody else — which on a map is a marker that is simply
                // not there.
                //
                // Worth knowing: the paging test does not catch the removal of
                // this line. At the handful of rows a test seeds, PostgreSQL
                // returns them in a stable order anyway, and the instability
                // only appears once the planner has a reason to choose
                // differently between the two queries. It is kept because it
                // is correct and costs nothing, not because a test is holding
                // it in place.
                .ThenBy(l => l.EmployeeId),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
