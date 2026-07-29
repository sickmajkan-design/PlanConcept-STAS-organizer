using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Vehicles.Models;

public class VehicleDto
{
    public Guid Id { get; init; }

    public string Brand { get; init; } = null!;

    public string Model { get; init; } = null!;

    public string RegistrationNumber { get; init; } = null!;

    public string? Vin { get; init; }

    public string FuelType { get; init; } = null!;

    public string Status { get; init; } = null!;

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public string? AssignedEmployeeNumber { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public class VehicleDtoMappingProfile : Profile
{
    public VehicleDtoMappingProfile()
    {
        CreateMap<Vehicle, VehicleDto>()
            .ForMember(d => d.FuelType, opt => opt.MapFrom(s => s.FuelType.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.AssignedEmployeeName, opt => opt.MapFrom(s =>
                s.AssignedEmployee != null
                    ? s.AssignedEmployee.FirstName + " " + s.AssignedEmployee.LastName
                    : null))
            .ForMember(d => d.AssignedEmployeeNumber, opt => opt.MapFrom(s =>
                s.AssignedEmployee != null ? s.AssignedEmployee.EmployeeNumber : null));
    }
}
