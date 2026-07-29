using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;

namespace Construction.API.Services;

/// <summary>
/// Exposes the authenticated user's identity (from JWT claims) to the
/// Application layer without leaking HTTP concerns into it.
/// </summary>
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
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email);

    public UserRole? Role
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
