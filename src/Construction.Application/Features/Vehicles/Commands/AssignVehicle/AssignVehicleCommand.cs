using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.AssignVehicle;

/// <summary>
/// Assigns (or reassigns) a vehicle to an employee. Vehicles under service
/// or out of service cannot be assigned.
/// </summary>
public record AssignVehicleCommand(Guid VehicleId, Guid EmployeeId) : IRequest<VehicleDto>;

public class AssignVehicleCommandHandler : IRequestHandler<AssignVehicleCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AssignVehicleCommandHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<VehicleDto> Handle(
        AssignVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        if (vehicle.Status is VehicleStatus.InService or VehicleStatus.OutOfService)
        {
            throw new ConflictException(
                $"The vehicle cannot be assigned while its status is '{vehicle.Status}'.");
        }

        if (vehicle.AssignedEmployeeId == request.EmployeeId)
        {
            throw new ConflictException("The vehicle is already assigned to this employee.");
        }

        vehicle.AssignedEmployeeId = employee.Id;
        vehicle.AssignedEmployee = employee;
        vehicle.Status = VehicleStatus.Assigned;

        await _context.SaveChangesAsync(cancellationToken);

        if (employee.User is { IsActive: true } user)
        {
            await _notificationService.NotifyUserAsync(
                user.Id,
                NotificationType.VehicleAssigned,
                "Vehicle assigned",
                $"Vehicle {vehicle.Brand} {vehicle.Model} ({vehicle.RegistrationNumber}) has been assigned to you.",
                new Dictionary<string, string> { ["vehicleId"] = vehicle.Id.ToString() },
                cancellationToken: cancellationToken);
        }

        return VehicleMapping.ToDto(vehicle);
    }
}
