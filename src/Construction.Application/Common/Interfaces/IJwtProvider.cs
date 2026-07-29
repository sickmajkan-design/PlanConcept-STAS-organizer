using Construction.Domain.Entities;

namespace Construction.Application.Common.Interfaces;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtProvider
{
    AccessTokenResult GenerateAccessToken(User user);

    /// <summary>Generates a cryptographically random opaque refresh token.</summary>
    string GenerateRefreshToken();

    /// <summary>Refresh token lifetime, from configuration.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}
