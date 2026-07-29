using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.DeleteVehicle;

/// <summary>
/// Soft-deletes a vehicle. Any employee assignment is cleared first so the
/// deleted vehicle no longer appears as held by anyone.
/// </summary>
public record DeleteVehicleCommand(Guid Id) : IRequest;

public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteVehicleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.Id);

        vehicle.AssignedEmployeeId = null;

        _context.Vehicles.Remove(vehicle);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
