using System.Security.Cryptography;
using Construction.Application.Common.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Construction.Infrastructure.Authentication;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hashing with a per-password random salt.
/// Stored format: {iterations}.{base64 salt}.{base64 subkey} — the iteration
/// count travels with the hash, so it can be raised later without breaking
/// existing credentials.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;

    // Built once, on first use, from a value that is never stored anywhere, so
    // verifying against it always fails and always costs a full derivation.
    private readonly Lazy<string> _dummyHash;

    public PasswordHasher()
    {
        _dummyHash = new Lazy<string>(() => Hash(Guid.NewGuid().ToString("N")));
    }

    public string DummyHash => _dummyHash.Value;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var subkey = Derive(password, salt, Iterations);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 3);

        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedSubkey;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedSubkey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualSubkey = Derive(password, salt, iterations);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, KeySizeBytes);
}
