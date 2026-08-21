using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands.CreateVehicle;

public record CreateVehicleCommand : VehicleCommandBase, IRequest<VehicleDto>;

public class CreateVehicleCommandValidator : VehicleCommandBaseValidator<CreateVehicleCommand>;

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    private readonly IApplicationDbContext _context;

    public CreateVehicleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleDto> Handle(
        CreateVehicleCommand request,
        CancellationToken cancellationToken)
    {
        var registrationNumber = request.RegistrationNumber.Trim().ToUpperInvariant();
        var vin = string.IsNullOrWhiteSpace(request.Vin)
            ? null
            : request.Vin.Trim().ToUpperInvariant();
        var qrCode = string.IsNullOrWhiteSpace(request.QrCode)
            ? await QrCodeGenerator.GenerateUniqueAsync(
                "VH",
                (candidate, ct) => _context.Vehicles.AnyAsync(v => v.QrCode == candidate, ct),
                cancellationToken)
            : request.QrCode.Trim();

        await VehicleUniqueness.EnsureUniqueAsync(
            _context, registrationNumber, vin, qrCode, excludeVehicleId: null, cancellationToken);

        var vehicle = new Vehicle
        {
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            RegistrationNumber = registrationNumber,
            Vin = vin,
            QrCode = qrCode,
            FuelType = request.FuelType,
            Status = request.Status
        };

        _context.Vehicles.Add(vehicle);

        await _context.SaveChangesAsync(cancellationToken);

        return VehicleMapping.ToDto(vehicle);
    }
}
