using AutoMapper;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using MediatR;

namespace Construction.Application.Features.Vehicles.Commands.CreateVehicle;

public record CreateVehicleCommand : VehicleCommandBase, IRequest<VehicleDto>;

public class CreateVehicleCommandValidator : VehicleCommandBaseValidator<CreateVehicleCommand>;

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateVehicleCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<VehicleDto> Handle(
        CreateVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var registrationNumber = request.RegistrationNumber.Trim().ToUpperInvariant();
        var vin = string.IsNullOrWhiteSpace(request.Vin)
            ? null
            : request.Vin.Trim().ToUpperInvariant();

        await VehicleUniqueness.EnsureUniqueAsync(
            _context, registrationNumber, vin, excludeVehicleId: null, cancellationToken);

        var vehicle = new Vehicle
        {
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            RegistrationNumber = registrationNumber,
            Vin = vin,
            FuelType = request.FuelType,
            Status = request.Status
        };

        _context.Vehicles.Add(vehicle);

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<VehicleDto>(vehicle);
    }
}
