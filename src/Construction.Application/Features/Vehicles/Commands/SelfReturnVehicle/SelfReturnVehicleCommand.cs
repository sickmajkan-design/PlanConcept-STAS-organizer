using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.SelfReturnVehicle;

/// <summary>
/// Lets the calling employee return a vehicle that is currently checked out
/// to them. Any vehicle checked out to someone else stays theirs to return.
/// </summary>
public record SelfReturnVehicleCommand(Guid VehicleId) : IRequest<VehicleDto>;

public class SelfReturnVehicleCommandHandler : IRequestHandler<SelfReturnVehicleCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SelfReturnVehicleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<VehicleDto> Handle(
        SelfReturnVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId
            ?? throw new ForbiddenAccessException("Only employees can return vehicles.");

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (vehicle.AssignedEmployeeId != employeeId)
        {
            throw new ConflictException("This vehicle is not checked out to you.");
        }

        vehicle.AssignedEmployeeId = null;
        vehicle.AssignedEmployee = null;

        if (vehicle.Status == VehicleStatus.Assigned)
        {
            vehicle.Status = VehicleStatus.Available;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return VehicleMapping.ToDto(vehicle);
    }
}
