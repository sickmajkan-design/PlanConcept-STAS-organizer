using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Authentication.Models;

public class UserDto
{
    public Guid Id { get; init; }

    public string Email { get; init; } = null!;

    public string Role { get; init; } = null!;

    public Guid? EmployeeId { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public DateTime? LastLoginAt { get; init; }
}

public class UserDtoMappingProfile : Profile
{
    public UserDtoMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()))
            .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.Employee != null ? s.Employee.FirstName : null))
            .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.Employee != null ? s.Employee.LastName : null));
    }
}
