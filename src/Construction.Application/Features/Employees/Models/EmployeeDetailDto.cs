using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Employees.Models;

public class EmployeeDetailDto : EmployeeDto
{
    public bool HasUserAccount { get; init; }

    public IReadOnlyCollection<EmployeeProjectAssignmentDto> Projects { get; init; } =
        Array.Empty<EmployeeProjectAssignmentDto>();
}

public class EmployeeProjectAssignmentDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public string ProjectStatus { get; init; } = null!;

    public DateTime AssignedAt { get; init; }
}

public class EmployeeDetailDtoMappingProfile : Profile
{
    public EmployeeDetailDtoMappingProfile()
    {
        CreateMap<Employee, EmployeeDetailDto>()
            .IncludeBase<Employee, EmployeeDto>()
            .ForMember(d => d.HasUserAccount, opt => opt.MapFrom(s => s.User != null))
            .ForMember(d => d.Projects, opt => opt.MapFrom(s => s.ProjectAssignments));

        CreateMap<EmployeeProject, EmployeeProjectAssignmentDto>()
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s => s.Project.Name))
            .ForMember(d => d.ProjectStatus, opt => opt.MapFrom(s => s.Project.Status.ToString()));
    }
}
