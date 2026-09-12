using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;

namespace Construction.API.Services;

/// <summary>
/// Exposes the authenticated user's identity (from JWT claims) to the
/// Application layer without leaking HTTP concerns into it.
/// </summary>
/// <remarks>
/// Checks <see cref="CurrentUserOverride"/> first. A background job has no
/// <see cref="HttpContext"/> to read claims from at all — the override is
/// how one stands in for the user whose scheduled action it is running,
/// rather than every property here silently reading as "nobody".
/// </remarks>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            if (CurrentUserOverride.Current is { } identity) return identity.UserId;

            var value = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? EmployeeId
    {
        get
        {
            if (CurrentUserOverride.Current is { } identity) return identity.EmployeeId;

            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue("employeeId");

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        CurrentUserOverride.Current?.Email
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email);

    public UserRole? Role
    {
        get
        {
            if (CurrentUserOverride.Current is { } identity) return identity.Role;

            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
