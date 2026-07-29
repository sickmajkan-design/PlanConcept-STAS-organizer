using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Commands;

/// <summary>
/// Registration number and VIN uniqueness checks shared by create and update.
/// Backed by the filtered unique indexes, so this exists to return a friendly
/// 409 instead of a database constraint violation.
/// </summary>
internal static class VehicleUniqueness
{
    public static async Task EnsureUniqueAsync(
        IApplicationDbContext context,
        string registrationNumber,
        string? vin,
        Guid? excludeVehicleId,
        CancellationToken cancellationToken)
    {
        var registrationTaken = await context.Vehicles.AnyAsync(
            v => v.RegistrationNumber == registrationNumber &&
                 (excludeVehicleId == null || v.Id != excludeVehicleId),
            cancellationToken);

        if (registrationTaken)
        {
            throw new ConflictException(
                $"Registration number '{registrationNumber}' is already in use.");
        }

        if (vin is null)
        {
            return;
        }

        var vinTaken = await context.Vehicles.AnyAsync(
            v => v.Vin == vin && (excludeVehicleId == null || v.Id != excludeVehicleId),
            cancellationToken);

        if (vinTaken)
        {
            throw new ConflictException($"VIN '{vin}' is already in use.");
        }
    }
}
