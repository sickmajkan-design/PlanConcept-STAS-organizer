namespace Construction.IntegrationTests;

/// <summary>
/// Shared plumbing for the integration tests. Every call runs in its own
/// scope so a handler never sees entities another call left tracked — the
/// same isolation a real HTTP request gets.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected IntegrationTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    protected DatabaseFixture Fixture { get; }

    /// <summary>
    /// Unfreezes the clock after every test.
    /// </summary>
    /// <remarks>
    /// The clock is a singleton on the fixture, so a test that freezes it and
    /// forgets to reset moves time for every test that runs afterwards. That
    /// used to be each author's job to remember, and the failure it produces —
    /// an unrelated test asserting on "now" — points nowhere near the test
    /// that caused it. xUnit disposes the test class after each test, so
    /// doing it here makes the leak impossible rather than merely discouraged.
    /// </remarks>
    public void Dispose()
    {
        using var scope = Fixture.CreateScope();
        scope.Clock.Reset();

        GC.SuppressFinalize(this);
    }

    protected async Task<T> InScope<T>(Func<TestScope, Task<T>> action)
    {
        using var scope = Fixture.CreateScope();
        return await action(scope);
    }

    /// <summary>Overload for commands that return nothing, such as the deletes.</summary>
    protected async Task InScope(Func<TestScope, Task> action)
    {
        using var scope = Fixture.CreateScope();
        await action(scope);
    }
}
