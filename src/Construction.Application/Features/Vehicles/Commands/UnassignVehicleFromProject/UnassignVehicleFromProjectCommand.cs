using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.UnassignVehicleFromProject;

/// <summary>Removes the vehicle's project assignment. Any employee assignment is kept.</summary>
public record UnassignVehicleFromProjectCommand(Guid VehicleId) : IRequest<VehicleDto>;

public class UnassignVehicleFromProjectCommandHandler
    : IRequestHandler<UnassignVehicleFromProjectCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;

    public UnassignVehicleFromProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleDto> Handle(
        UnassignVehicleFromProjectCommand request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.AssignedEmployee)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (vehicle.AssignedProjectId is null)
        {
            throw new ConflictException("The vehicle is not assigned to any project.");
        }

        vehicle.AssignedProjectId = null;
        vehicle.AssignedProject = null;

        await _context.SaveChangesAsync(cancellationToken);

        return VehicleMapping.ToDto(vehicle);
    }
}
