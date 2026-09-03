using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetVehicleCosts;

/// <summary>What the fleet cost over a period, and what it drank.</summary>
/// <remarks>
/// Fuel is split out from the rest because it is the only line that says
/// anything about how a vehicle is being used rather than merely what it cost.
/// A van whose litres per 100 km jumps is either developing a fault or having
/// its fuel card used elsewhere, and neither shows up in a running total.
/// </remarks>
public record GetVehicleCostsQuery : IRequest<VehicleCostReportDto>
{
    public const int MaxDays = 732;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public Guid? VehicleId { get; init; }
}

public class GetVehicleCostsQueryValidator : AbstractValidator<GetVehicleCostsQuery>
{
    public GetVehicleCostsQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetVehicleCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetVehicleCostsQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetVehicleCostsQueryHandler
    : IRequestHandler<GetVehicleCostsQuery, VehicleCostReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetVehicleCostsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<VehicleCostReportDto> Handle(
        GetVehicleCostsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see cost reports.");
        }

        // One grouped query for the whole fleet. Odometer bounds come back
        // alongside the money so the distance needs no second round trip.
        var grouped = await _context.VehicleExpenses
            .AsNoTracking()
            .Where(e => request.VehicleId == null || e.VehicleId == request.VehicleId)
            .Where(e => e.OccurredOn >= request.From && e.OccurredOn <= request.To)
            .GroupBy(e => new
            {
                e.VehicleId,
                Name = e.Vehicle.Brand + " " + e.Vehicle.Model
                    + " (" + e.Vehicle.RegistrationNumber + ")"
            })
            .Select(g => new
            {
                g.Key.VehicleId,
                g.Key.Name,
                FuelCost = g.Where(e => e.Kind == VehicleExpenseKind.Fuel)
                    .Sum(e => (decimal?)e.Amount) ?? 0m,
                Litres = g.Where(e => e.Kind == VehicleExpenseKind.Fuel)
                    .Sum(e => e.Litres) ?? 0m,
                ServiceCost = g.Where(e => e.Kind == VehicleExpenseKind.Service
                        || e.Kind == VehicleExpenseKind.Repair)
                    .Sum(e => (decimal?)e.Amount) ?? 0m,
                TotalCost = g.Sum(e => (decimal?)e.Amount) ?? 0m,
                FirstOdometer = g.Min(e => e.OdometerKm),
                LastOdometer = g.Max(e => e.OdometerKm)
            })
            .ToListAsync(cancellationToken);

        // Rental/lease cost is a separate source — a vehicle can carry one
        // with no VehicleExpense rows at all in the period, so it needs its
        // own vehicle lookup rather than riding along with the group above.
        var rentalRates = await _context.VehicleRentalRates
            .AsNoTracking()
            .Where(r => request.VehicleId == null || r.VehicleId == request.VehicleId)
            .Where(r => r.StartDate <= request.To && (r.EndDate == null || r.EndDate >= request.From))
            .Select(r => new
            {
                r.VehicleId,
                Name = r.Vehicle.Brand + " " + r.Vehicle.Model
                    + " (" + r.Vehicle.RegistrationNumber + ")",
                r.StartDate,
                r.EndDate,
                r.MonthlyAmount
            })
            .ToListAsync(cancellationToken);

        // A 30-day month is a deliberate approximation, the same one a flat
        // "per day" reading of a monthly figure always is — the point is a
        // consistent number to compare period over period, not an invoice
        // reconciliation.
        var rentalByVehicle = rentalRates
            .GroupBy(r => new { r.VehicleId, r.Name })
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r =>
                {
                    var start = r.StartDate > request.From ? r.StartDate : request.From;
                    var end = r.EndDate is { } e && e < request.To ? e : request.To;
                    var days = end.DayNumber - start.DayNumber + 1;
                    return days > 0 ? days * (r.MonthlyAmount / 30m) : 0m;
                }));

        var vehicleNames = grouped
            .Select(v => (v.VehicleId, v.Name))
            .Concat(rentalByVehicle.Keys.Select(k => (k.VehicleId, k.Name)))
            .GroupBy(v => v.VehicleId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var vehicleIds = vehicleNames.Keys.ToHashSet();
        var expensesByVehicle = grouped.ToDictionary(v => v.VehicleId);

        var rows = vehicleIds
            .Select(vehicleId =>
            {
                var v = expensesByVehicle.GetValueOrDefault(vehicleId);
                var rentalCost = rentalByVehicle
                    .Where(kv => kv.Key.VehicleId == vehicleId)
                    .Select(kv => kv.Value)
                    .FirstOrDefault();

                var distance = v?.FirstOdometer is { } first && v.LastOdometer is { } last
                    && last > first
                    ? last - first
                    : (int?)null;

                var fuelCost = v?.FuelCost ?? 0m;
                var litres = v?.Litres ?? 0m;
                var serviceCost = v?.ServiceCost ?? 0m;
                var totalCost = v?.TotalCost ?? 0m;

                return new VehicleCostRowDto
                {
                    VehicleId = vehicleId,
                    VehicleName = vehicleNames[vehicleId],
                    FuelCost = decimal.Round(fuelCost, 2),
                    Litres = decimal.Round(litres, 3),
                    ServiceCost = decimal.Round(serviceCost, 2),
                    OtherCost = decimal.Round(totalCost - fuelCost - serviceCost, 2),
                    RentalCost = decimal.Round(rentalCost, 2),
                    Total = decimal.Round(totalCost + rentalCost, 2),
                    DistanceKm = distance,
                    // Only when both halves are real. A single fill-up gives
                    // no distance, and dividing by a distance of nothing would
                    // produce a headline figure out of one data point.
                    LitresPer100Km = distance is { } km && km > 0 && litres > 0
                        ? decimal.Round(litres * 100m / km, 2)
                        : null
                };
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.VehicleName)
            .ToList();

        return new VehicleCostReportDto
        {
            From = request.From,
            To = request.To,
            Rows = rows,
            Total = rows.Sum(r => r.Total),
            TotalFuelCost = rows.Sum(r => r.FuelCost),
            TotalLitres = rows.Sum(r => r.Litres),
            TotalRentalCost = rows.Sum(r => r.RentalCost)
        };
    }
}
