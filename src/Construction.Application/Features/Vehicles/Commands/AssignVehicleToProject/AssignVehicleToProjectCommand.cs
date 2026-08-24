using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.AssignVehicleToProject;

/// <summary>
/// Places the vehicle on a project (or moves it to another one). Independent
/// of any employee assignment — a vehicle can sit on a project and be held by
/// an employee at the same time.
/// </summary>
public record AssignVehicleToProjectCommand(Guid VehicleId, Guid ProjectId) : IRequest<VehicleDto>;

public class AssignVehicleToProjectCommandHandler
    : IRequestHandler<AssignVehicleToProjectCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;

    public AssignVehicleToProjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleDto> Handle(
        AssignVehicleToProjectCommand request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .Include(v => v.AssignedEmployee)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        if (vehicle.Status is VehicleStatus.InService or VehicleStatus.OutOfService)
        {
            throw new ConflictException("A vehicle that is in service or out of service cannot be assigned.");
        }

        if (vehicle.AssignedProjectId == request.ProjectId)
        {
            throw new ConflictException("The vehicle is already assigned to this project.");
        }

        vehicle.AssignedProjectId = project.Id;
        vehicle.AssignedProject = project;

        await _context.SaveChangesAsync(cancellationToken);

        return VehicleMapping.ToDto(vehicle);
    }
}
