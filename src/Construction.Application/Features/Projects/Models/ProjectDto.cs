using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Projects.Models;

public class ProjectDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string? Client { get; init; }

    public string? Address { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string Status { get; init; } = null!;

    public int EmployeeCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public class ProjectDtoMappingProfile : Profile
{
    public ProjectDtoMappingProfile()
    {
        CreateMap<Project, ProjectDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.EmployeeCount, opt => opt.MapFrom(s => s.EmployeeAssignments.Count));
    }
}
