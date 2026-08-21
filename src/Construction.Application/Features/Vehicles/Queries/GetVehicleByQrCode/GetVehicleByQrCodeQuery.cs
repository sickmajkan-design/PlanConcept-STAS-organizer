using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Vehicles.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Queries.GetVehicleByQrCode;

/// <summary>
/// Looks a vehicle up by the value encoded in its QR label — the endpoint the
/// mobile app calls after scanning a vehicle on site.
/// </summary>
public record GetVehicleByQrCodeQuery(string QrCode) : IRequest<VehicleDto>;

public class GetVehicleByQrCodeQueryValidator : AbstractValidator<GetVehicleByQrCodeQuery>
{
    public GetVehicleByQrCodeQueryValidator()
    {
        RuleFor(x => x.QrCode)
            .NotEmpty().WithMessage("QR code is required.")
            .MaximumLength(256);
    }
}

public class GetVehicleByQrCodeQueryHandler : IRequestHandler<GetVehicleByQrCodeQuery, VehicleDto>
{
    private readonly IApplicationDbContext _context;

    public GetVehicleByQrCodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleDto> Handle(GetVehicleByQrCodeQuery request, CancellationToken cancellationToken)
    {
        var qrCode = request.QrCode.Trim();

        var vehicle = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.QrCode == qrCode)
            .Select(VehicleMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return vehicle ?? throw new NotFoundException($"No vehicle found for QR code '{qrCode}'.");
    }
}
