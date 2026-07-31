using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Stands in for the API layer's JWT-claims-backed implementation. Tests set
/// the acting identity directly, which is the same thing the real service
/// derives from the token.
/// </summary>
public sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }

    public Guid? EmployeeId { get; set; }

    public string? Email { get; set; }

    public UserRole? Role { get; set; }

    public string? IpAddress { get; set; } = "127.0.0.1";

    public void SignInAs(Guid userId, UserRole role, Guid? employeeId = null, string? email = null)
    {
        UserId = userId;
        Role = role;
        EmployeeId = employeeId;
        Email = email;
    }

    public void SignOut()
    {
        UserId = null;
        Role = null;
        EmployeeId = null;
        Email = null;
    }
}
