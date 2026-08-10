using System.Linq.Expressions;
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

/// <summary>
/// How a <see cref="User"/> becomes an <see cref="UserDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class UserMapping
{
    public static readonly Expression<Func<User, UserDto>> Projection = user =>
        new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            LockoutEndsAt = user.LockoutEndsAt,
            EmployeeId = user.EmployeeId,
            EmployeeName = user.Employee != null
                ? user.Employee.FirstName + " " + user.Employee.LastName
                : null,
            CreatedAt = user.CreatedAt,
        };

    private static readonly Func<User, UserDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static UserDto ToDto(User user) => Compiled(user);
}
