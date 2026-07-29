using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Projects.Models;

public class ProjectDetailDto : ProjectDto
{
    public IReadOnlyCollection<ProjectEmployeeDto> Employees { get; init; } =
        Array.Empty<ProjectEmployeeDto>();
}

public class ProjectEmployeeDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeNumber { get; init; } = null!;

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;

    public string Status { get; init; } = null!;

    public DateTime AssignedAt { get; init; }
}

public class ProjectDetailDtoMappingProfile : Profile
{
    public ProjectDetailDtoMappingProfile()
    {
        CreateMap<Project, ProjectDetailDto>()
            .IncludeBase<Project, ProjectDto>()
            .ForMember(d => d.Employees, opt => opt.MapFrom(s => s.EmployeeAssignments));

        CreateMap<EmployeeProject, ProjectEmployeeDto>()
            .ForMember(d => d.EmployeeNumber, opt => opt.MapFrom(s => s.Employee.EmployeeNumber))
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.Employee.FirstName + " " + s.Employee.LastName))
            .ForMember(d => d.Position, opt => opt.MapFrom(s => s.Employee.Position))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Employee.Status.ToString()));
    }
}
