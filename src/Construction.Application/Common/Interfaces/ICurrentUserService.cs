using Construction.Domain.Enums;

namespace Construction.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>The employee record linked to the account, when there is one.</summary>
    Guid? EmployeeId { get; }

    string? Email { get; }

    UserRole? Role { get; }

    string? IpAddress { get; }
}
