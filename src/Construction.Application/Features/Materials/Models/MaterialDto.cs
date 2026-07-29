using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Materials.Models;

public class MaterialDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Unit { get; init; } = null!;

    public decimal Quantity { get; init; }

    public string? Warehouse { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public DateTime LastUpdated { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public class MaterialDtoMappingProfile : Profile
{
    public MaterialDtoMappingProfile()
    {
        CreateMap<Material, MaterialDto>()
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s =>
                s.Project != null ? s.Project.Name : null));
    }
}
