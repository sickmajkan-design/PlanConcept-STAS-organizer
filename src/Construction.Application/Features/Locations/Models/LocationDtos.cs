using AutoMapper;
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

public class LocationMappingProfile : Profile
{
    public LocationMappingProfile()
    {
        CreateMap<LocationRecord, LocationRecordDto>();

        CreateMap<LocationRecord, EmployeeLocationDto>()
            .ForMember(d => d.EmployeeNumber, opt => opt.MapFrom(s => s.Employee.EmployeeNumber))
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.Employee.FirstName + " " + s.Employee.LastName))
            .ForMember(d => d.Position, opt => opt.MapFrom(s => s.Employee.Position));
    }
}
