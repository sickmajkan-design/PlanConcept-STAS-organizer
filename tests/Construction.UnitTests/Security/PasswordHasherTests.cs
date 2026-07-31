using Construction.Infrastructure.Authentication;

namespace Construction.UnitTests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_accepts_the_password_that_produced_the_hash()
    {
        var hash = _hasher.Hash("Admin123!");

        Assert.True(_hasher.Verify("Admin123!", hash));
    }

    [Fact]
    public void Verify_rejects_a_different_password()
    {
        var hash = _hasher.Hash("Admin123!");

        Assert.False(_hasher.Verify("Admin123", hash));
        Assert.False(_hasher.Verify("admin123!", hash));
        Assert.False(_hasher.Verify(string.Empty, hash));
    }

    [Fact]
    public void Hash_salts_every_password_so_equal_passwords_differ()
    {
        var first = _hasher.Hash("SamePassword1!");
        var second = _hasher.Hash("SamePassword1!");

        Assert.NotEqual(first, second);
        // Both must still verify — the salt travels inside the hash.
        Assert.True(_hasher.Verify("SamePassword1!", first));
        Assert.True(_hasher.Verify("SamePassword1!", second));
    }

    [Fact]
    public void Hash_records_the_iteration_count_so_it_can_be_raised_later()
    {
        var parts = _hasher.Hash("Admin123!").Split('.');

        Assert.Equal(3, parts.Length);
        Assert.Equal(100_000, int.Parse(parts[0]));
    }

    [Fact]
    public void Verify_still_accepts_a_hash_written_with_a_lower_iteration_count()
    {
        // Simulates a credential stored before the work factor was raised:
        // the count in the hash is what must be used to re-derive it.
        var current = _hasher.Hash("Legacy123!");
        var parts = current.Split('.', 3);
        var weaker = $"1000.{parts[1]}.{parts[2]}";

        // The subkey was derived with 100k iterations, so re-deriving with
        // 1000 must not match — proving the stored count actually drives it.
        Assert.False(_hasher.Verify("Legacy123!", weaker));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abc.def.ghi")]
    [InlineData("100000.!!!notbase64!!!.xyz")]
    [InlineData("100000.onlytwoparts")]
    public void Verify_returns_false_for_a_malformed_hash_instead_of_throwing(string malformed)
    {
        Assert.False(_hasher.Verify("Admin123!", malformed));
    }
}
