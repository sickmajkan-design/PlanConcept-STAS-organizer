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

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public string? AssignedEmployeeNumber { get; init; }

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
            AssignedEmployeeId = vehicle.AssignedEmployeeId,
            AssignedEmployeeName = vehicle.AssignedEmployee != null
                ? vehicle.AssignedEmployee.FirstName + " " + vehicle.AssignedEmployee.LastName
                : null,
            AssignedEmployeeNumber = vehicle.AssignedEmployee != null
                ? vehicle.AssignedEmployee.EmployeeNumber
                : null,
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt,
        };

    private static readonly Func<Vehicle, VehicleDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static VehicleDto ToDto(Vehicle vehicle) => Compiled(vehicle);
}
