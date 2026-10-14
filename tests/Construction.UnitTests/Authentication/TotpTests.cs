using System.Text;
using Construction.Application.Features.Authentication.Mfa;

namespace Construction.UnitTests.Authentication;

public class TotpTests
{
    // RFC 6238 appendix B, SHA-1: the 8-digit values there, cut to the last 6 digits.
    private static readonly byte[] RfcSecret = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void Matches_the_published_test_vectors(long unixSeconds, string expected)
    {
        Assert.Equal(expected, Totp.Compute(RfcSecret, unixSeconds / Totp.StepSeconds));
    }

    [Fact]
    public void Encodes_base32_like_an_authenticator_app_expects()
    {
        Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", Totp.ToBase32(RfcSecret));
    }

    [Fact]
    public void Accepts_the_current_code_and_reports_its_step()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109).UtcDateTime;

        var step = Totp.Verify(RfcSecret, "081804", now, lastUsedStep: null);

        Assert.Equal(1111111109L / Totp.StepSeconds, step);
    }

    [Fact]
    public void Accepts_a_code_one_step_old_for_clock_drift()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109 + Totp.StepSeconds).UtcDateTime;

        Assert.NotNull(Totp.Verify(RfcSecret, "081804", now, null));
    }

    [Fact]
    public void Rejects_a_code_two_steps_old()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109 + 2 * Totp.StepSeconds).UtcDateTime;

        Assert.Null(Totp.Verify(RfcSecret, "081804", now, null));
    }

    [Fact]
    public void Refuses_a_code_that_was_already_used()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109).UtcDateTime;
        var step = Totp.Verify(RfcSecret, "081804", now, null);

        Assert.Null(Totp.Verify(RfcSecret, "081804", now, lastUsedStep: step));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    public void Rejects_malformed_codes(string? code)
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109).UtcDateTime;

        Assert.Null(Totp.Verify(RfcSecret, code, now, null));
    }

    [Fact]
    public void Ignores_spaces_and_dashes_people_type()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1111111109).UtcDateTime;

        Assert.NotNull(Totp.Verify(RfcSecret, "081 804", now, null));
        Assert.NotNull(Totp.Verify(RfcSecret, "081-804", now, null));
    }

    [Fact]
    public void Builds_a_provisioning_link()
    {
        var uri = Totp.ProvisioningUri("STAS", "ana@example.com", RfcSecret);

        Assert.StartsWith("otpauth://totp/STAS:ana%40example.com?secret=GEZDGNBVGY3TQOJQ", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }
}
