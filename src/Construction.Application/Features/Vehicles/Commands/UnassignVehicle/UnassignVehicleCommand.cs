using AutoMapper;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.UnassignVehicle;

/// <summary>
/// Removes the vehicle's employee assignment and returns it to the pool.
/// </summary>
public record UnassignVehicleCommand(Guid VehicleId) : IRequest<VehicleDto>;

public class UnassignVehicleCommandHandler : IRequestHandler<UnassignVehicleCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UnassignVehicleCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<VehicleDto> Handle(
        UnassignVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (vehicle.AssignedEmployeeId is null)
        {
            throw new ConflictException("The vehicle is not assigned to any employee.");
        }

        vehicle.AssignedEmployeeId = null;
        vehicle.AssignedEmployee = null;

        if (vehicle.Status == VehicleStatus.Assigned)
        {
            vehicle.Status = VehicleStatus.Available;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<VehicleDto>(vehicle);
    }
}
