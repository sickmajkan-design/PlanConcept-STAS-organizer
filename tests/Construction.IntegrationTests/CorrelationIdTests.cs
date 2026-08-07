using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.API.Middleware;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The id that makes "it broke this morning" answerable.
/// </summary>
/// <remarks>
/// Over HTTP against the real pipeline, because the whole value is in the
/// ordering: the middleware has to run before the request logger and before
/// anything that starts a response, and a unit test of the middleware in
/// isolation would prove none of that.
/// </remarks>
[Collection(ApiCollection.Name)]
public class CorrelationIdTests
{
    private readonly ApiFixture _api;

    public CorrelationIdTests(ApiFixture api)
    {
        _api = api;
    }

    private static string? HeaderOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values)
            ? values.FirstOrDefault()
            : null;

    [Fact]
    public async Task Every_response_carries_one()
    {
        using var client = _api.ClientAs(UserRole.Admin);

        var response = await client.GetAsync("/api/employees");

        Assert.False(string.IsNullOrWhiteSpace(HeaderOf(response)));
    }

    [Fact]
    public async Task An_anonymous_refusal_carries_one_too()
    {
        // The failures are exactly when somebody needs the id, and a 401 is
        // written by middleware that runs before any controller.
        using var client = _api.AnonymousClient();

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(HeaderOf(response)));
    }

    [Fact]
    public async Task Two_requests_get_two_different_ids()
    {
        using var client = _api.ClientAs(UserRole.Admin);

        var first = await client.GetAsync("/api/employees");
        var second = await client.GetAsync("/api/employees");

        Assert.NotEqual(HeaderOf(first), HeaderOf(second));
    }

    [Fact]
    public async Task A_caller_s_own_id_is_kept_so_a_chain_of_calls_shares_one()
    {
        using var client = _api.ClientAs(UserRole.Admin);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/employees");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "abc-123_XYZ");

        var response = await client.SendAsync(request);

        Assert.Equal("abc-123_XYZ", HeaderOf(response));
    }

    [Theory]
    // A newline forges log entries: everything after it looks like a separate,
    // authentic line to whatever reads the log.
    [InlineData("has\nnewline")]
    [InlineData("has\rreturn")]
    // Kilobytes of junk on every line is a slow denial of service against
    // whoever pays per gigabyte ingested.
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("")]
    [InlineData("spaces here")]
    [InlineData("{\"json\":\"injection\"}")]
    public async Task A_dangerous_id_is_replaced_rather_than_trusted(string supplied)
    {
        using var client = _api.ClientAs(UserRole.Admin);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/employees");

        // TryAddWithoutValidation, because HttpClient refuses to put a newline
        // in a header at all — and a hostile caller is not using HttpClient.
        request.Headers.TryAddWithoutValidation(
            CorrelationIdMiddleware.HeaderName, supplied);

        var response = await client.SendAsync(request);

        var returned = HeaderOf(response);

        Assert.NotNull(returned);
        Assert.NotEqual(supplied, returned);

        // The replacement is a plain token, whatever arrived.
        Assert.Matches("^[A-Za-z0-9_-]{1,64}$", returned);
    }

    [Fact]
    public async Task A_failure_reports_the_same_id_the_header_carries()
    {
        // The point of the whole thing: a user quotes what the screen showed,
        // and it is the same string the log lines were written under.
        using var client = _api.ClientAs(UserRole.Worker);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(problem.TryGetProperty("correlationId", out var reported));
        Assert.Equal(HeaderOf(response), reported.GetString());
    }

    [Fact]
    public async Task The_health_check_carries_one_as_well()
    {
        using var client = _api.AnonymousClient();

        var response = await client.GetAsync("/health");

        Assert.False(string.IsNullOrWhiteSpace(HeaderOf(response)));
    }
}
