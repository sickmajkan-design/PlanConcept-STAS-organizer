using System.Security.Cryptography;

namespace Construction.Application.Common;

/// <summary>
/// Generates short, printable QR label values for tools and vehicles that
/// were created without an explicit code.
/// </summary>
internal static class QrCodeGenerator
{
    // Excludes visually ambiguous characters (0/O, 1/I) so a printed label is
    // never misread when it has to be typed in by hand as a fallback.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static async Task<string> GenerateUniqueAsync(
        string prefix,
        Func<string, CancellationToken, Task<bool>> existsAsync,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = Generate(prefix);

            if (!await existsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique QR code.");
    }

    private static string Generate(string prefix)
    {
        Span<char> code = stackalloc char[8];

        for (var i = 0; i < code.Length; i++)
        {
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return $"{prefix}-{new string(code)}";
    }
}
