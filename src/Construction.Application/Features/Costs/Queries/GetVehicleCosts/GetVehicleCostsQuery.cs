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

        var rows = grouped
            .Select(v =>
            {
                var distance = v.FirstOdometer is { } first && v.LastOdometer is { } last
                    && last > first
                    ? last - first
                    : (int?)null;

                return new VehicleCostRowDto
                {
                    VehicleId = v.VehicleId,
                    VehicleName = v.Name,
                    FuelCost = decimal.Round(v.FuelCost, 2),
                    Litres = decimal.Round(v.Litres, 3),
                    ServiceCost = decimal.Round(v.ServiceCost, 2),
                    OtherCost = decimal.Round(v.TotalCost - v.FuelCost - v.ServiceCost, 2),
                    Total = decimal.Round(v.TotalCost, 2),
                    DistanceKm = distance,
                    // Only when both halves are real. A single fill-up gives
                    // no distance, and dividing by a distance of nothing would
                    // produce a headline figure out of one data point.
                    LitresPer100Km = distance is { } km && km > 0 && v.Litres > 0
                        ? decimal.Round(v.Litres * 100m / km, 2)
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
            TotalLitres = rows.Sum(r => r.Litres)
        };
    }
}
