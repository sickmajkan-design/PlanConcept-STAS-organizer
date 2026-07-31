using Construction.Application.Common.Security;

namespace Construction.UnitTests.Security;

public class TokenHasherTests
{
    [Fact]
    public void Sha256_is_deterministic_so_a_presented_token_can_be_looked_up()
    {
        Assert.Equal(TokenHasher.Sha256("token-value"), TokenHasher.Sha256("token-value"));
    }

    [Fact]
    public void Sha256_distinguishes_different_tokens()
    {
        Assert.NotEqual(TokenHasher.Sha256("token-a"), TokenHasher.Sha256("token-b"));
    }

    [Fact]
    public void Sha256_never_returns_the_raw_token()
    {
        const string raw = "a-refresh-token-that-must-not-be-stored";

        var hash = TokenHasher.Sha256(raw);

        Assert.DoesNotContain(raw, hash, StringComparison.Ordinal);
        Assert.Equal(64, hash.Length); // SHA-256 as hex
    }

    [Fact]
    public void Sha256_matches_the_known_digest_for_a_fixed_input()
    {
        // Guards against an accidental algorithm or encoding change, which
        // would silently invalidate every stored token hash.
        Assert.Equal(
            "9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08",
            TokenHasher.Sha256("test"));
    }
}
