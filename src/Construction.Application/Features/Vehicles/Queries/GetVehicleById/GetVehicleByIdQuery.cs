using Construction.Application.Common;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Queries.GetVehicleById;

public record GetVehicleByIdQuery(Guid Id) : IRequest<VehicleDto>;

public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehicleByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleDto> Handle(
        GetVehicleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.Id == request.Id)
            .Select(VehicleMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.Id);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var ownProjects = await SiteScope.OwnProjectIdsAsync(_context, _currentUserService, today, cancellationToken);

        // Not found rather than forbidden, as for a project: a foreman or project manager is
        // not told that a vehicle outside their fleet exists.
        if (ownProjects is not null
            && (vehicle.AssignedProjectId is not { } projectId || !ownProjects.Contains(projectId))
            && vehicle.AssignedEmployeeId != _currentUserService.EmployeeId)
        {
            throw new NotFoundException(nameof(Vehicle), request.Id);
        }

        return vehicle;
    }
}
