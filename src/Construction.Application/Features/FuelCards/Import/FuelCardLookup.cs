using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelCards.Import;

/// <summary>Loads every active fuel card once per import request, keyed for matching.</summary>
internal static class FuelCardLookup
{
    public static async Task<Dictionary<string, FuelCardLookupEntry>> LoadByCardNumber(
        IApplicationDbContext context, CancellationToken cancellationToken) =>
        await context.FuelCards
            .AsNoTracking()
            .Select(c => new
            {
                c.CardNumber,
                c.VehicleId,
                VehicleName = c.Vehicle.Brand + " " + c.Vehicle.Model + " (" + c.Vehicle.RegistrationNumber + ")",
                c.Provider,
            })
            .ToDictionaryAsync(
                c => c.CardNumber.ToLowerInvariant(),
                c => new FuelCardLookupEntry(c.VehicleId, c.VehicleName, c.Provider),
                cancellationToken);
}
