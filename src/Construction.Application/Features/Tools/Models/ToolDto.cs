using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Tools.Models;

public class ToolDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Category { get; init; }

    public string? SerialNumber { get; init; }

    public string? QrCode { get; init; }

    public string Status { get; init; } = null!;

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public string? AssignedEmployeeNumber { get; init; }

    public Guid? AssignedProjectId { get; init; }

    public string? AssignedProjectName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public class ToolDtoMappingProfile : Profile
{
    public ToolDtoMappingProfile()
    {
        CreateMap<Tool, ToolDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.AssignedEmployeeName, opt => opt.MapFrom(s =>
                s.AssignedEmployee != null
                    ? s.AssignedEmployee.FirstName + " " + s.AssignedEmployee.LastName
                    : null))
            .ForMember(d => d.AssignedEmployeeNumber, opt => opt.MapFrom(s =>
                s.AssignedEmployee != null ? s.AssignedEmployee.EmployeeNumber : null))
            .ForMember(d => d.AssignedProjectName, opt => opt.MapFrom(s =>
                s.AssignedProject != null ? s.AssignedProject.Name : null));
    }
}
