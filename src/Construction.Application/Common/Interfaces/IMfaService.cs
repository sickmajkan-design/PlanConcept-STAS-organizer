using Construction.Domain.Enums;

namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Holds the authenticator secrets safely and says who must use a second factor.
/// </summary>
public interface IMfaService
{
    /// <summary>
    /// False when no encryption key is configured. Nobody can enrol then: a secret
    /// must never be stored in the clear because a setting was forgotten.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>Name shown in the authenticator app next to the account.</summary>
    string Issuer { get; }

    /// <summary>Whether sign-in for this role is expected to carry a second factor.</summary>
    bool IsRequiredFor(UserRole role);

    /// <summary>Encrypts a secret, bound to the account it belongs to.</summary>
    string Protect(byte[] secret, Guid userId);

    /// <summary>Decrypts a stored secret; null when it was tampered with or the key changed.</summary>
    byte[]? Unprotect(string protectedSecret, Guid userId);
}
