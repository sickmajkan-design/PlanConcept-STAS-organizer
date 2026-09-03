using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Vehicles.Models;

public class VehicleDto
{
    public Guid Id { get; init; }

    public string Brand { get; init; } = null!;

    public string Model { get; init; } = null!;

    public string RegistrationNumber { get; init; } = null!;

    public string? Vin { get; init; }

    public string? QrCode { get; init; }

    public string FuelType { get; init; } = null!;

    public string Status { get; init; } = null!;

    public string OwnershipType { get; init; } = null!;

    /// <summary>Set when a rental/lease rate is currently in force (EndDate null). Null for an owned vehicle, or one with no rate on file.</summary>
    public decimal? CurrentRentalMonthlyAmount { get; init; }

    public string? CurrentRentalProvider { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public string? AssignedEmployeeNumber { get; init; }

    public Guid? AssignedProjectId { get; init; }

    public string? AssignedProjectName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Vehicle"/> becomes an <see cref="VehicleDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class VehicleMapping
{
    public static readonly Expression<Func<Vehicle, VehicleDto>> Projection = vehicle =>
        new VehicleDto
        {
            Id = vehicle.Id,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            RegistrationNumber = vehicle.RegistrationNumber,
            Vin = vehicle.Vin,
            QrCode = vehicle.QrCode,
            FuelType = vehicle.FuelType.ToString(),
            Status = vehicle.Status.ToString(),
            OwnershipType = vehicle.OwnershipType.ToString(),
            CurrentRentalMonthlyAmount = vehicle.RentalRates
                .Where(r => r.EndDate == null)
                .Select(r => (decimal?)r.MonthlyAmount)
                .FirstOrDefault(),
            CurrentRentalProvider = vehicle.RentalRates
                .Where(r => r.EndDate == null)
                .Select(r => r.Provider)
                .FirstOrDefault(),
            AssignedEmployeeId = vehicle.AssignedEmployeeId,
            AssignedEmployeeName = vehicle.AssignedEmployee != null
                ? vehicle.AssignedEmployee.FirstName + " " + vehicle.AssignedEmployee.LastName
                : null,
            AssignedEmployeeNumber = vehicle.AssignedEmployee != null
                ? vehicle.AssignedEmployee.EmployeeNumber
                : null,
            AssignedProjectId = vehicle.AssignedProjectId,
            AssignedProjectName = vehicle.AssignedProject != null ? vehicle.AssignedProject.Name : null,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt,
        };

    private static readonly Func<Vehicle, VehicleDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static VehicleDto ToDto(Vehicle vehicle) => Compiled(vehicle);
}
