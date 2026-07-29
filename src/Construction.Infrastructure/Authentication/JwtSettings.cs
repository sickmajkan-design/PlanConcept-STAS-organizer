namespace Construction.Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    /// <summary>
    /// HMAC-SHA256 signing key. Minimum 32 characters; supply via
    /// environment variable (JwtSettings__SecretKey) in production.
    /// </summary>
    public string SecretKey { get; set; } = null!;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 7;
}
