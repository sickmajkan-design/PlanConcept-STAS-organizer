using System.Security.Cryptography;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Construction.Infrastructure.Authentication;

public class MfaSettings
{
    public const string SectionName = "Auth:Mfa";

    /// <summary>Base64 of 32 random bytes. Supplied as <c>Auth__Mfa__EncryptionKey</c>, never committed.</summary>
    public string EncryptionKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Construction";

    /// <summary>Role names that are expected to use a second factor, e.g. SuperAdmin and Admin.</summary>
    public string[] RequiredRoles { get; set; } = [];
}

/// <summary>
/// AES-256-GCM over the authenticator secret, with the account id as associated
/// data so a ciphertext copied onto another row does not decrypt.
/// </summary>
public class MfaService : IMfaService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[]? _key;
    private readonly HashSet<UserRole> _required;

    public MfaService(IOptions<MfaSettings> options)
    {
        var settings = options.Value;

        if (!string.IsNullOrWhiteSpace(settings.EncryptionKey))
        {
            try
            {
                var key = Convert.FromBase64String(settings.EncryptionKey);
                _key = key.Length == 32 ? key : null;
            }
            catch (FormatException)
            {
                _key = null;
            }
        }

        Issuer = string.IsNullOrWhiteSpace(settings.Issuer) ? "Construction" : settings.Issuer.Trim();

        _required = settings.RequiredRoles
            .Select(name => Enum.TryParse<UserRole>(name, ignoreCase: true, out var role) ? role : (UserRole?)null)
            .Where(role => role is not null)
            .Select(role => role!.Value)
            .ToHashSet();
    }

    public bool IsAvailable => _key is not null;

    public string Issuer { get; }

    public bool IsRequiredFor(UserRole role) => _required.Contains(role);

    public string Protect(byte[] secret, Guid userId)
    {
        var key = _key ?? throw new InvalidOperationException("No MFA encryption key is configured.");

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[secret.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, secret, cipher, tag, userId.ToByteArray());

        var packed = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(packed, 0);
        tag.CopyTo(packed, NonceSize);
        cipher.CopyTo(packed, NonceSize + TagSize);

        return Convert.ToBase64String(packed);
    }

    public byte[]? Unprotect(string protectedSecret, Guid userId)
    {
        if (_key is null)
        {
            return null;
        }

        try
        {
            var packed = Convert.FromBase64String(protectedSecret);
            if (packed.Length <= NonceSize + TagSize)
            {
                return null;
            }

            var nonce = packed.AsSpan(0, NonceSize);
            var tag = packed.AsSpan(NonceSize, TagSize);
            var cipher = packed.AsSpan(NonceSize + TagSize);
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain, userId.ToByteArray());

            return plain;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return null;
        }
    }
}
