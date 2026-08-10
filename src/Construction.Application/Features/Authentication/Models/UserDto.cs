using System.Linq.Expressions;
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
            EmployeeId = user.EmployeeId,
            FirstName = user.Employee != null ? user.Employee.FirstName : null,
            LastName = user.Employee != null ? user.Employee.LastName : null,
            LastLoginAt = user.LastLoginAt,
        };

    private static readonly Func<User, UserDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static UserDto ToDto(User user) => Compiled(user);
}
