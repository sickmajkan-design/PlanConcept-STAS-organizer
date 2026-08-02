using AutoMapper;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Users.Models;

/// <summary>
/// An account as the admin panel sees it.
///
/// Mapped explicitly rather than from the entity wholesale, so the password
/// hash cannot reach a response by someone adding a field to <see cref="User"/>.
/// </summary>
public class UserDto
{
    public Guid Id { get; init; }

    public string Email { get; init; } = null!;

    public string Role { get; init; } = null!;

    public bool IsActive { get; init; }

    public DateTime? LastLoginAt { get; init; }

    /// <summary>Set while the account is barred after repeated failed sign-ins.</summary>
    public DateTime? LockoutEndsAt { get; init; }

    public Guid? EmployeeId { get; init; }

    /// <summary>Name of the linked employee, so the list needs no second call.</summary>
    public string? EmployeeName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class UserDtoMappingProfile : Profile
{
    public UserDtoMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()))
            .ForMember(d => d.EmployeeName, opt => opt.MapFrom(s =>
                s.Employee != null ? s.Employee.FirstName + " " + s.Employee.LastName : null));
    }
}
