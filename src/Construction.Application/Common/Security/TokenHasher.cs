using System.Security.Cryptography;
using System.Text;

namespace Construction.Application.Common.Security;

/// <summary>
/// Deterministic SHA-256 hashing for opaque tokens (refresh / password-reset).
/// Raw tokens are never persisted; only these hashes are stored and looked up.
/// </summary>
public static class TokenHasher
{
    public static string Sha256(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
