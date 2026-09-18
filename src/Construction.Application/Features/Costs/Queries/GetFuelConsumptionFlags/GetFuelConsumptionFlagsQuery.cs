using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetFuelConsumptionFlags;

/// <summary>
/// Fill-ups whose consumption looks wrong next to that same vehicle's own
/// history — a leak, a bad injector, or fuel going somewhere it shouldn't.
/// </summary>
/// <remarks>
/// A flat "L/100km over X" threshold can't work here: a van and a truck have
/// nothing in common, and a van bought this year has nothing in common with
/// one from a decade ago. What's comparable is a vehicle against itself, which
/// is why this only ever measures deviation from a vehicle's own prior
/// average, never from anything else.
/// </remarks>
public record GetFuelConsumptionFlagsQuery : IRequest<List<FuelConsumptionFlagDto>>
{
    public Guid? VehicleId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetFuelConsumptionFlagsQueryHandler
    : IRequestHandler<GetFuelConsumptionFlagsQuery, List<FuelConsumptionFlagDto>>
{
    /// <summary>
    /// How many of a vehicle's own prior fill-ups must already be on record
    /// before a new one can be judged against them.
    /// </summary>
    /// <remarks>
    /// Below this, "average" is really just "the last reading or two" and
    /// flagging against it would mostly catch ordinary variation — a cold
    /// start, a short trip, a different driver — not a real problem.
    /// </remarks>
    private const int MinimumBaselineReadings = 3;

    /// <summary>How far from its own average a fill-up has to be to get flagged.</summary>
    private const decimal DeviationThreshold = 0.25m;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFuelConsumptionFlagsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<FuelConsumptionFlagDto>> Handle(
        GetFuelConsumptionFlagsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see vehicle costs.");
        }

        // Both readings are opt-in per fill-up (see VehicleExpense.OdometerKm),
        // so most of the history for a real account has gaps. A pair with
        // either missing simply cannot yield a distance, and is excluded
        // rather than guessed at.
        var query = _context.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.Kind == VehicleExpenseKind.Fuel
                && e.OdometerKm != null
                && e.Litres != null
                && e.Litres > 0);

        if (request.VehicleId is { } vehicleId)
        {
            query = query.Where(e => e.VehicleId == vehicleId);
        }

        if (request.From is { } from)
        {
            query = query.Where(e => e.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(e => e.OccurredOn <= to);
        }

        var fillUps = await query
            .OrderBy(e => e.VehicleId)
            .ThenBy(e => e.OccurredOn)
            .ThenBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Select(e => new
            {
                e.Id,
                e.VehicleId,
                VehicleName = e.Vehicle.Brand + " " + e.Vehicle.Model
                    + " (" + e.Vehicle.RegistrationNumber + ")",
                e.OccurredOn,
                OdometerKm = e.OdometerKm!.Value,
                Litres = e.Litres!.Value,
            })
            .ToListAsync(cancellationToken);

        var flags = new List<FuelConsumptionFlagDto>();

        foreach (var vehicleFillUps in fillUps.GroupBy(f => f.VehicleId))
        {
            // Consumption for a fill-up is what it took to cover the distance
            // since the *previous* one — so the first reading for a vehicle
            // never yields a value, it only starts the first pair.
            var readings = new List<decimal>();

            var ordered = vehicleFillUps.ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                var previous = ordered[i - 1];
                var current = ordered[i];
                var distanceKm = current.OdometerKm - previous.OdometerKm;

                // An odometer that went backwards or didn't move means a
                // reset, a replaced cluster, or a typo — not zero consumption.
                if (distanceKm <= 0)
                {
                    continue;
                }

                var litresPer100Km = current.Litres / distanceKm * 100m;

                // Judged against the readings already gathered for this
                // vehicle — never the value itself, and never a later one —
                // so a flag reflects what was known at the time, not hindsight.
                if (readings.Count >= MinimumBaselineReadings)
                {
                    var average = readings.Average();
                    var deviation = average == 0 ? 0 : (litresPer100Km - average) / average;

                    if (Math.Abs(deviation) >= DeviationThreshold)
                    {
                        flags.Add(new FuelConsumptionFlagDto
                        {
                            ExpenseId = current.Id,
                            VehicleId = current.VehicleId,
                            VehicleName = current.VehicleName,
                            OccurredOn = current.OccurredOn,
                            DistanceKm = distanceKm,
                            Litres = current.Litres,
                            LitresPer100Km = Math.Round(litresPer100Km, 1),
                            VehicleAverageLitresPer100Km = Math.Round(average, 1),
                            DeviationPercent = Math.Round(deviation * 100m, 0),
                        });
                    }
                }

                readings.Add(litresPer100Km);
            }
        }

        return flags
            .OrderByDescending(f => f.OccurredOn)
            .ThenByDescending(f => f.ExpenseId)
            .ToList();
    }
}
