using System.Net;
using System.Net.Http.Json;
using Construction.API.Extensions;

namespace Construction.IntegrationTests;

/// <summary>
/// The endpoint the two clients report their own crashes to.
/// </summary>
/// <remarks>
/// <para>
/// Server faults have been findable for a while. Client faults had nowhere to
/// go: a panel showing "something went wrong" and a phone showing a crash
/// panel were each telling one person something nobody else would ever hear.
/// </para>
/// <para>
/// What is asserted here is mostly about the endpoint being unauthenticated
/// without being a liability — because it is both a log writer and open to
/// anyone who can reach the API.
/// </para>
/// </remarks>
[Collection(ApiCollection.Name)]
public class ClientErrorReportingTests
{
    private readonly ApiFixture _api;

    public ClientErrorReportingTests(ApiFixture api)
    {
        _api = api;
    }

    private static object Report(
        string app = "admin",
        string message = "TypeError: cannot read properties of undefined",
        string? stack = "at Grid (Grid.tsx:42)") =>
        new
        {
            app,
            message,
            kind = "TypeError",
            stack,
            route = "/employees",
            version = "1.4.0",
            platform = "Mozilla/5.0",
        };

    /// <summary>
    /// Accepted without a token.
    /// </summary>
    /// <remarks>
    /// The report worth having most is the one from a sign-in screen that will
    /// not load, and a client that cannot authenticate cannot report that it
    /// cannot authenticate. Requiring a token here would silence exactly the
    /// failure this exists for.
    /// </remarks>
    [Fact]
    public async Task A_signed_out_client_can_report_a_crash()
    {
        using var client = _api.AnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/client-errors", Report());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    /// <summary>
    /// The answer carries the correlation id.
    /// </summary>
    /// <remarks>
    /// So the operator reading "something went wrong, quote this" and the
    /// person reading the log are quoting the same string. That is the whole
    /// reason the id exists, and a report that did not return one would leave
    /// the two halves unjoinable.
    /// </remarks>
    [Fact]
    public async Task The_report_comes_back_with_the_id_the_log_line_carries()
    {
        using var client = _api.AnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/client-errors", Report());

        var body = await response.Content.ReadFromJsonAsync<CorrelationEnvelope>();

        Assert.False(string.IsNullOrWhiteSpace(body?.CorrelationId));
        Assert.Equal(
            response.Headers.GetValues("X-Correlation-Id").Single(),
            body!.CorrelationId);
    }

    /// <summary>
    /// A stack longer than the cap is refused rather than written.
    /// </summary>
    /// <remarks>
    /// This is an unauthenticated endpoint whose job is to write to the log.
    /// Without a bound, the size of what it will write down is chosen by
    /// whoever calls it, and filling a disk becomes a POST.
    /// </remarks>
    [Fact]
    public async Task An_enormous_stack_is_refused()
    {
        using var client = _api.AnonymousClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/client-errors",
            Report(stack: new string('x', 20_000)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_report_with_no_message_is_refused()
    {
        using var client = _api.AnonymousClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/client-errors",
            new { app = "admin", message = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class CorrelationEnvelope
    {
        public string? CorrelationId { get; init; }
    }
}
