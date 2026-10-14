using System.Security.Cryptography;
using System.Text;

namespace Construction.Application.Features.Authentication.Mfa;

/// <summary>
/// Single-use codes for someone who has lost their authenticator. Shown once at
/// enrolment, stored only as hashes.
/// </summary>
public static class RecoveryCodes
{
    public const int Count = 10;

    // No 0/O or 1/I/L: these are read off a printed sheet and typed by hand.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Length = 10;

    public static IReadOnlyList<string> Generate()
    {
        var codes = new List<string>(Count);

        for (var i = 0; i < Count; i++)
        {
            var chars = new StringBuilder(Length + 1);
            for (var c = 0; c < Length; c++)
            {
                chars.Append(Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]);
            }

            chars.Insert(Length / 2, '-');
            codes.Add(chars.ToString());
        }

        return codes;
    }

    /// <summary>The form that is hashed: upper case, no separators, so how it was typed does not matter.</summary>
    public static string Normalize(string code) =>
        code.Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

    public static bool LooksLikeRecoveryCode(string code) =>
        Normalize(code).Length == Length && Normalize(code).All(Alphabet.Contains);
}
