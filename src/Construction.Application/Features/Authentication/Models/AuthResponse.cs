namespace Construction.Application.Features.Authentication.Models;

public class AuthResponse
{
    public string AccessToken { get; init; } = null!;

    public DateTime AccessTokenExpiresAt { get; init; }

    public string RefreshToken { get; init; } = null!;

    public DateTime RefreshTokenExpiresAt { get; init; }

    public UserDto User { get; init; } = null!;
}
