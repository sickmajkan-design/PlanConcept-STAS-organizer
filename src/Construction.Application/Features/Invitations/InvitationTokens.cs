using System.Security.Cryptography;
using System.Text;

namespace Construction.Application.Features.Invitations;

/// <summary>Creating and hashing invitation tokens. Only the hash is ever stored.</summary>
public static class InvitationTokens
{
    /// <summary>How long an invitation link works.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    /// <summary>256 bits from the system generator, URL-safe.</summary>
    public static string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
