using Construction.Application.Common.Interfaces;

namespace Construction.IntegrationTests;

/// <summary>
/// Clock the tests can move, so token-expiry paths can be exercised without
/// waiting. Defaults to the real current time, so tests that do not care
/// about time behave normally.
/// </summary>
public sealed class MutableDateTimeProvider : IDateTimeProvider
{
    private DateTime? _fixed;

    public DateTime UtcNow => _fixed ?? DateTime.UtcNow;

    public void FreezeAt(DateTime utc) => _fixed = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => _fixed = UtcNow.Add(by);

    public void Reset() => _fixed = null;
}
