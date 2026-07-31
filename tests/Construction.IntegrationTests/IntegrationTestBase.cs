namespace Construction.IntegrationTests;

/// <summary>
/// Shared plumbing for the integration tests. Every call runs in its own
/// scope so a handler never sees entities another call left tracked — the
/// same isolation a real HTTP request gets.
/// </summary>
public abstract class IntegrationTestBase
{
    protected IntegrationTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    protected DatabaseFixture Fixture { get; }

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
