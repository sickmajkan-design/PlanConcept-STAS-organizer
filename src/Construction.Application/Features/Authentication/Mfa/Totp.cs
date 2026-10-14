using System.Security.Cryptography;
using System.Text;

namespace Construction.Application.Features.Authentication.Mfa;

/// <summary>
/// Time-based one-time passwords (RFC 6238, HMAC-SHA1, 30 s, 6 digits), the
/// scheme every authenticator app speaks. Pure functions, so the RFC's own test
/// vectors can pin it.
/// </summary>
public static class Totp
{
    public const int StepSeconds = 30;
    public const int Digits = 6;

    /// <summary>Steps either side of "now" that are still accepted, for clock drift.</summary>
    public const int Window = 1;

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static byte[] NewSecret() => RandomNumberGenerator.GetBytes(20);

    public static long StepOf(DateTime utc) =>
        new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds() / StepSeconds;

    public static string Compute(byte[] secret, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counter[i] = (byte)(step & 0xFF);
            step >>= 8;
        }

        Span<byte> hash = stackalloc byte[20];
        HMACSHA1.HashData(secret, counter, hash);

        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];

        var modulus = 1;
        for (var i = 0; i < Digits; i++)
        {
            modulus *= 10;
        }

        return (binary % modulus).ToString().PadLeft(Digits, '0');
    }

    /// <summary>
    /// Checks a code against the steps around <paramref name="utcNow"/>. Returns
    /// the step it matched, or null. A step at or below <paramref name="lastUsedStep"/>
    /// is refused, so one code cannot be replayed.
    /// </summary>
    public static long? Verify(byte[] secret, string? code, DateTime utcNow, long? lastUsedStep)
    {
        if (code is null)
        {
            return null;
        }

        var trimmed = code.Replace(" ", string.Empty).Replace("-", string.Empty);
        if (trimmed.Length != Digits || !trimmed.All(char.IsAsciiDigit))
        {
            return null;
        }

        var current = StepOf(utcNow);
        long? matched = null;

        // Every candidate is compared, none short-circuits, so timing does not
        // say which step (or whether any) matched.
        for (var step = current - Window; step <= current + Window; step++)
        {
            var expected = Compute(secret, step);
            var equal = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(trimmed));

            if (equal && (lastUsedStep is null || step > lastUsedStep))
            {
                matched = step;
            }
        }

        return matched;
    }

    public static string ToBase32(ReadOnlySpan<byte> data)
    {
        var result = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                result.Append(Base32Alphabet[(buffer >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            result.Append(Base32Alphabet[(buffer << (5 - bits)) & 0x1F]);
        }

        return result.ToString();
    }

    /// <summary>The <c>otpauth://</c> link an authenticator app reads from a QR code.</summary>
    public static string ProvisioningUri(string issuer, string account, byte[] secret) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}"
        + $"?secret={ToBase32(secret)}&issuer={Uri.EscapeDataString(issuer)}"
        + $"&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
}
