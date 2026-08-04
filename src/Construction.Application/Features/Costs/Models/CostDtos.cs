using AutoMapper;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Costs.Models;

public class EmployeeRateDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public decimal HourlyRate { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }

    public string? SetByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class MaterialMovementDto
{
    public Guid Id { get; init; }

    public Guid MaterialId { get; init; }

    public string MaterialName { get; init; } = null!;

    public string Unit { get; init; } = null!;

    public MaterialMovementKind Kind { get; init; }

    public decimal Quantity { get; init; }

    public decimal? UnitPrice { get; init; }

    public decimal? TotalCost { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class VehicleExpenseDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string VehicleName { get; init; } = null!;

    public VehicleExpenseKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public decimal? Litres { get; init; }

    public decimal? PricePerLitre { get; init; }

    public int? OdometerKm { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class CostMappingProfile : Profile
{
    public CostMappingProfile()
    {
        CreateMap<EmployeeRate, EmployeeRateDto>()
            .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s =>
                s.Employee.FirstName + " " + s.Employee.LastName))
            .ForMember(d => d.SetByName, opt => opt.MapFrom(s =>
                s.SetByUser != null ? s.SetByUser.Email : null));

        CreateMap<MaterialMovement, MaterialMovementDto>()
            .ForMember(d => d.MaterialName, opt => opt.MapFrom(s => s.Material.Name))
            .ForMember(d => d.Unit, opt => opt.MapFrom(s => s.Material.Unit))
            // Spelled out rather than taken from the entity's computed
            // property, which ProjectTo cannot turn into SQL.
            .ForMember(d => d.TotalCost, opt => opt.MapFrom(s =>
                s.UnitPrice != null
                    ? s.UnitPrice * (s.Quantity < 0 ? -s.Quantity : s.Quantity)
                    : (decimal?)null))
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s =>
                s.Project != null ? s.Project.Name : null))
            .ForMember(d => d.RecordedByName, opt => opt.MapFrom(s =>
                s.RecordedByUser != null ? s.RecordedByUser.Email : null));

        CreateMap<VehicleExpense, VehicleExpenseDto>()
            .ForMember(d => d.VehicleName, opt => opt.MapFrom(s =>
                s.Vehicle.Brand + " " + s.Vehicle.Model + " (" + s.Vehicle.RegistrationNumber + ")"))
            .ForMember(d => d.PricePerLitre, opt => opt.MapFrom(s =>
                s.Litres != null && s.Litres > 0 ? s.Amount / s.Litres : (decimal?)null))
            .ForMember(d => d.RecordedByName, opt => opt.MapFrom(s =>
                s.RecordedByUser != null ? s.RecordedByUser.Email : null));
    }
}
