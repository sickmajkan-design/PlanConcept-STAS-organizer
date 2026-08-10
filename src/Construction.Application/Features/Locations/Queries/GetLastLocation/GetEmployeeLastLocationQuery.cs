using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Locations.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Locations.Queries.GetLastLocation;

/// <summary>Last known position of one employee.</summary>
public record GetEmployeeLastLocationQuery(Guid EmployeeId) : IRequest<EmployeeLocationDto>;

public class GetEmployeeLastLocationQueryHandler
    : IRequestHandler<GetEmployeeLastLocationQuery, EmployeeLocationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetEmployeeLastLocationQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EmployeeLocationDto> Handle(
        GetEmployeeLastLocationQuery request,
        CancellationToken cancellationToken)
    {
        // Was "does this employee exist"; now "does this employee exist and
        // may the caller look at them", which is one question because the
        // answer has to be the same either way.
        // Same answer as "not there" on purpose. Telling a foreman that an
        // employee exists but is not theirs to look at confirms the employee
        // exists, which is most of what somebody probing for a colleague's
        // whereabouts wants to learn.
        var visible = await CrewVisibility.CanSeeAsync(
            _context.Employees,
            _context.EmployeeProjects,
            _currentUserService,
            request.EmployeeId,
            DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            cancellationToken);

        if (!visible)
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var location = await _context.LocationRecords
            .AsNoTracking()
            .Where(l => l.EmployeeId == request.EmployeeId)
            .OrderByDescending(l => l.Timestamp)
            .Select(EmployeeLocationMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return location
            ?? throw new NotFoundException(
                $"No location has been reported yet for employee '{request.EmployeeId}'.");
    }
}
