using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Fakes;

/// <summary>A signed-in person, stated rather than derived from a token.</summary>
public sealed class StubCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; } = Guid.NewGuid();

    public Guid? EmployeeId { get; set; } = Guid.NewGuid();

    public string? Email { get; set; } = "ana@construction.local";

    public UserRole? Role { get; set; } = UserRole.Admin;

    public string? IpAddress { get; set; } = "10.0.0.2";
}
