using Construction.Application.Common.Interfaces;
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
public record GetCurrentLocationsQuery : IRequest<IReadOnlyList<EmployeeLocationDto>>
{
    /// <summary>Only employees assigned to this project.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Ignore pings older than this many minutes (default: no limit).</summary>
    public int? MaxAgeMinutes { get; init; }

    /// <summary>Include employees who are not in Active status (default false).</summary>
    public bool IncludeInactive { get; init; }
}

public class GetCurrentLocationsQueryValidator : AbstractValidator<GetCurrentLocationsQuery>
{
    public GetCurrentLocationsQueryValidator()
    {
        RuleFor(x => x.MaxAgeMinutes)
            .GreaterThanOrEqualTo(1).WithMessage("MaxAgeMinutes must be at least 1.")
            .When(x => x.MaxAgeMinutes is not null);
    }
}

public class GetCurrentLocationsQueryHandler
    : IRequestHandler<GetCurrentLocationsQuery, IReadOnlyList<EmployeeLocationDto>>
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

    public async Task<IReadOnlyList<EmployeeLocationDto>> Handle(
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
        var locations = await employees
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
            })
            .ToListAsync(cancellationToken);

        return locations;
    }
}
