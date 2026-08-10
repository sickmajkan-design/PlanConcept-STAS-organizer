using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Locations.Models;

/// <summary>A single stored GPS ping.</summary>
public class LocationRecordDto
{
    public long Id { get; init; }

    public Guid EmployeeId { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double? Accuracy { get; init; }

    /// <summary>When the device captured the fix (UTC).</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>When the server received the ping (UTC).</summary>
    public DateTime ReceivedAt { get; init; }
}

/// <summary>An employee's most recent position, as shown on the admin map.</summary>
public class EmployeeLocationDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeNumber { get; init; } = null!;

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double? Accuracy { get; init; }

    public DateTime Timestamp { get; init; }
}

/// <summary>How a <see cref="LocationRecord"/> becomes a <see cref="LocationRecordDto"/>.</summary>
/// <remarks>See <c>EmployeeMapping</c> for the convention these all follow.</remarks>
public static class LocationRecordMapping
{
    public static readonly Expression<Func<LocationRecord, LocationRecordDto>> Projection =
        record => new LocationRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            Latitude = record.Latitude,
            Longitude = record.Longitude,
            Accuracy = record.Accuracy,
            Timestamp = record.Timestamp,
            ReceivedAt = record.ReceivedAt,
        };

    private static readonly Func<LocationRecord, LocationRecordDto> Compiled = Projection.Compile();

    public static LocationRecordDto ToDto(LocationRecord record) => Compiled(record);
}

/// <summary>How a <see cref="LocationRecord"/> becomes an <see cref="EmployeeLocationDto"/>.</summary>
public static class EmployeeLocationMapping
{
    public static readonly Expression<Func<LocationRecord, EmployeeLocationDto>> Projection =
        record => new EmployeeLocationDto
        {
            EmployeeId = record.EmployeeId,
            EmployeeNumber = record.Employee.EmployeeNumber,
            FullName = record.Employee.FirstName + " " + record.Employee.LastName,
            Position = record.Employee.Position,
            Latitude = record.Latitude,
            Longitude = record.Longitude,
            Accuracy = record.Accuracy,
            Timestamp = record.Timestamp,
        };

    private static readonly Func<LocationRecord, EmployeeLocationDto> Compiled =
        Projection.Compile();

    public static EmployeeLocationDto ToDto(LocationRecord record) => Compiled(record);
}
