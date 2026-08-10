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
/// <para>
/// <b>Any new class that builds its own <c>WebApplicationFactory</c> must join
/// this collection.</b> One that does not will very likely still pass locally:
/// a class added without it went green nine times on a four-core machine — and
/// on one core — and failed on CI both times, taking 161 unrelated tests with
/// it because the fixture the rest of the suite shares was the one that lost
/// the race. The symptom names no culprit, so the rule has to be remembered
/// here rather than discovered there.
/// </para>
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public class StandaloneHostCollection
{
    public const string Name = "standalone-host";
}
