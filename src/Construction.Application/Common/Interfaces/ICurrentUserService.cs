using Construction.Domain.Enums;

namespace Construction.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    UserRole? Role { get; }

    string? IpAddress { get; }
}
