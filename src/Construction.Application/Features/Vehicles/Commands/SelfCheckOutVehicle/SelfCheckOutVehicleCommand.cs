using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.SelfCheckOutVehicle;

/// <summary>
/// Lets the calling employee check a vehicle out to themselves after
/// scanning its QR label. Unlike <c>AssignVehicleCommand</c>, the target
/// employee is always the caller — never a route/body parameter — so any
/// authenticated employee may use it, not just a Project Manager or above.
/// </summary>
public record SelfCheckOutVehicleCommand(Guid VehicleId) : IRequest<VehicleDto>;

public class SelfCheckOutVehicleCommandHandler : IRequestHandler<SelfCheckOutVehicleCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SelfCheckOutVehicleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<VehicleDto> Handle(
        SelfCheckOutVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId
            ?? throw new ForbiddenAccessException("Only employees can check out vehicles.");

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (vehicle.Status is VehicleStatus.InService or VehicleStatus.OutOfService)
        {
            throw new ConflictException(
                $"The vehicle cannot be checked out while its status is '{vehicle.Status}'.");
        }

        if (vehicle.AssignedEmployeeId == employeeId)
        {
            throw new ConflictException("The vehicle is already checked out to you.");
        }

        if (vehicle.AssignedEmployeeId is not null)
        {
            throw new ConflictException("The vehicle is already checked out to someone else.");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), employeeId);

        vehicle.AssignedEmployeeId = employee.Id;
        vehicle.AssignedEmployee = employee;
        vehicle.Status = VehicleStatus.Assigned;

        await _context.SaveChangesAsync(cancellationToken);

        return VehicleMapping.ToDto(vehicle);
    }
}
