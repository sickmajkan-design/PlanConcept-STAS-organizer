using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.UpdateVehicle;

public record UpdateVehicleCommand : VehicleCommandBase, IRequest<VehicleDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }
}

public class UpdateVehicleCommandValidator : VehicleCommandBaseValidator<UpdateVehicleCommand>;

public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateVehicleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleDto> Handle(
        UpdateVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.AssignedEmployee)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.Id);

        var registrationNumber = request.RegistrationNumber.Trim().ToUpperInvariant();
        var vin = string.IsNullOrWhiteSpace(request.Vin)
            ? null
            : request.Vin.Trim().ToUpperInvariant();

        await VehicleUniqueness.EnsureUniqueAsync(
            _context, registrationNumber, vin, request.Id, cancellationToken);

        if (request.Status != VehicleStatus.Assigned && vehicle.AssignedEmployeeId is not null)
        {
            throw new ConflictException(
                "The vehicle is assigned to an employee; unassign it before changing its status.");
        }

        vehicle.Brand = request.Brand.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.RegistrationNumber = registrationNumber;
        vehicle.Vin = vin;
        vehicle.FuelType = request.FuelType;
        vehicle.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return VehicleMapping.ToDto(vehicle);
    }
}
