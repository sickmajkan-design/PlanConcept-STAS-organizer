namespace Construction.Application.Features.Authentication.Models;

public class AuthResponse
{
    public string AccessToken { get; init; } = null!;

    public DateTime AccessTokenExpiresAt { get; init; }

    public string RefreshToken { get; init; } = null!;

    public DateTime RefreshTokenExpiresAt { get; init; }

    public UserDto User { get; init; } = null!;

    /// <summary>
    /// True when the password was right but a code from the authenticator is still
    /// needed. The token fields above are then empty and <see cref="MfaToken"/>
    /// goes to <c>POST /auth/mfa/verify</c> together with the code.
    /// </summary>
    public bool MfaRequired { get; init; }

    public string? MfaToken { get; init; }

    /// <summary>
    /// The account's role is expected to use a second factor and has not set one up.
    /// The sign-in itself succeeded; the panel should send the person to set it up.
    /// </summary>
    public bool MfaEnrollmentRequired { get; init; }
}
