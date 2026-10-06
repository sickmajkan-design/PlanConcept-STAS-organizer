using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
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
        var tdNumber = request.TdNumber.Trim().ToUpperInvariant();
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
            _context, registrationNumber, tdNumber, vin, qrCode, excludeVehicleId: null, cancellationToken);

        var branch = await Branches.BranchLookup.LoadAsync(_context, request.BranchId, cancellationToken);

        var vehicle = new Vehicle
        {
            BranchId = request.BranchId,
            Branch = branch,
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            RegistrationNumber = registrationNumber,
            TdNumber = tdNumber,
            Vin = vin,
            QrCode = qrCode,
            GpsProvider = string.IsNullOrWhiteSpace(request.GpsProvider)
                ? null
                : request.GpsProvider.Trim(),
            GpsTrackingUrl = string.IsNullOrWhiteSpace(request.GpsTrackingUrl)
                ? null
                : request.GpsTrackingUrl.Trim(),
            RegistrationValidUntil = request.RegistrationValidUntil,
            TechnicalInspectionValidUntil = request.TechnicalInspectionValidUntil,
            InsuranceValidUntil = request.InsuranceValidUntil,
            NextServiceDue = request.NextServiceDue,
            RentedUntil = request.OwnershipType == VehicleOwnershipType.Owned ? null : request.RentedUntil,
            FuelType = request.FuelType,
            Status = request.Status,
            OwnershipType = request.OwnershipType
        };

        _context.Vehicles.Add(vehicle);

        await _context.SaveChangesAsync(cancellationToken);

        return VehicleMapping.ToDto(vehicle);
    }
}
