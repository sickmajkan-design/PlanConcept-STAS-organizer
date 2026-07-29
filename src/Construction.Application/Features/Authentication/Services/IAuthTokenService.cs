using Construction.Application.Features.Authentication.Models;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Authentication.Services;

/// <summary>
/// Issues the access-token / refresh-token pair for an authenticated user.
/// Shared by the login and refresh-token flows so rotation logic lives in one place.
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Creates and persists a new refresh token, generates a JWT access token
    /// and returns the full auth payload. Does not call SaveChanges — the
    /// calling handler owns the unit of work.
    /// </summary>
    AuthResponse IssueTokens(User user, string? ipAddress, out RefreshToken issuedRefreshToken);
}
