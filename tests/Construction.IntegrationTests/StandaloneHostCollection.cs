namespace Construction.IntegrationTests;

/// <summary>
/// The test classes that host their own copy of the API instead of sharing
/// <see cref="ApiFixture"/>.
/// </summary>
/// <remarks>
/// <para>
/// They exist because what they assert is about startup itself — an
/// unreachable database, a malformed CORS origin — which cannot be arranged on
/// an application that is already running.
/// </para>
/// <para>
/// The collection carries no fixture. Its only job is to stop these classes
/// running at the same time as each other. <c>WebApplicationFactory</c> starts
/// a top-level-statements <c>Program</c> through
/// <c>HostFactoryResolver</c>, which listens on a process-wide diagnostic
/// source to catch the host on its way out of <c>Build()</c>; two of them
/// building at once can each pick up the other's host, or neither. That
/// surfaces as "The entry point exited without ever building an IHost" in
/// whichever test lost, with nothing wrong in the code under test — and it was
/// reproducible three runs out of three before these were serialised.
/// </para>
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public class StandaloneHostCollection
{
    public const string Name = "standalone-host";
}
