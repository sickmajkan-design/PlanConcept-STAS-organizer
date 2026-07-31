using Construction.Application.Common.Interfaces;

namespace Construction.UnitTests.Fakes;

/// <summary>
/// Clock frozen at a known instant, so tests that assert on expiry windows
/// are deterministic. Hand-written rather than mocked — the interface has a
/// single member and a fake keeps the test project free of a mocking
/// dependency.
/// </summary>
public sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public static readonly DateTime DefaultNow =
        new(2026, 7, 31, 10, 0, 0, DateTimeKind.Utc);

    public FixedDateTimeProvider(DateTime? utcNow = null)
    {
        UtcNow = utcNow ?? DefaultNow;
    }

    public DateTime UtcNow { get; set; }
}
