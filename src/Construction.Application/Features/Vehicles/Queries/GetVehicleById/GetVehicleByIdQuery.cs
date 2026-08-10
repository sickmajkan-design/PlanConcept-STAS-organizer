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

    public GetVehicleByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleDto> Handle(
        GetVehicleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.Id == request.Id)
            .Select(VehicleMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return vehicle ?? throw new NotFoundException(nameof(Vehicle), request.Id);
    }
}
