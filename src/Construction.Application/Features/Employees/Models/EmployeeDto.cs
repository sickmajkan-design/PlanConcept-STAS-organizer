using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Employees.Models;

public class EmployeeDto
{
    public Guid Id { get; init; }

    public string EmployeeNumber { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string FullName { get; init; } = null!;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public DateOnly EmploymentDate { get; init; }

    public string Position { get; init; } = null!;

    public string Status { get; init; } = null!;

    public string? PhotoUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public class EmployeeDtoMappingProfile : Profile
{
    public EmployeeDtoMappingProfile()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}
