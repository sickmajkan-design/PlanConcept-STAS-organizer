using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Authentication.Models;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Authentication.Services;

public class AuthTokenService : IAuthTokenService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtProvider _jwtProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthTokenService(
        IApplicationDbContext context,
        IJwtProvider jwtProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _jwtProvider = jwtProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public AuthResponse IssueTokens(User user, string? ipAddress, out RefreshToken issuedRefreshToken)
    {
        var accessToken = _jwtProvider.GenerateAccessToken(user);
        var rawRefreshToken = _jwtProvider.GenerateRefreshToken();
        var refreshTokenExpiresAt = _dateTimeProvider.UtcNow.Add(_jwtProvider.RefreshTokenLifetime);

        issuedRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(rawRefreshToken),
            ExpiresAt = refreshTokenExpiresAt,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(issuedRefreshToken);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAtUtc,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = UserMapping.ToDto(user)
        };
    }
}
