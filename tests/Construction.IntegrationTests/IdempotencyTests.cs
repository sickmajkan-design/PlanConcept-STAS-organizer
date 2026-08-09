using System.Net;
using System.Net.Http.Json;
using Construction.API.Filters;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The retried request, over real HTTP against a real database.
/// </summary>
/// <remarks>
/// <para>
/// This has to be an HTTP test rather than a handler test: the whole mechanism
/// is a request header, an action filter and a unique index, and none of those
/// is on the path a MediatR call takes. A test that sent the command twice
/// through the handler would prove nothing about the endpoint.
/// </para>
/// <para>
/// The failure being prevented is not hypothetical. A foreman taps "consume
/// 40 bags" at the edge of coverage, the response is lost on the way back, and
/// the app retries. Without a key the second request is indistinguishable from
/// a genuine second consumption and the stock drops by eighty — silently, and
/// only discovered at the next count.
/// </para>
/// </remarks>
[Collection(ApiCollection.Name)]
public class IdempotencyTests
{
    private readonly ApiFixture _api;

    public IdempotencyTests(ApiFixture api)
    {
        _api = api;
    }

    private static string NewKey() => Guid.NewGuid().ToString("N");

    private async Task<Guid> SeedMaterialAsync(HttpClient client, decimal quantity)
    {
        var response = await client.PostAsJsonAsync("/api/v1/materials", new
        {
            name = $"Cement {Guid.NewGuid():N}"[..24],
            unit = "bag",
            quantity,
        });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<MaterialResponse>();

        return created!.Id;
    }

    private Task<decimal> QuantityOf(Guid id) =>
        _api.InScope(context => context.Materials
            .Where(m => m.Id == id)
            .Select(m => m.Quantity)
            .SingleAsync());

    private static HttpRequestMessage Adjust(Guid id, decimal change, string? key)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, $"/api/v1/materials/{id}/adjust")
        {
            Content = JsonContent.Create(new { change, reason = "site count" }),
        };

        if (key is not null)
        {
            request.Headers.Add(IdempotencyFilter.HeaderName, key);
        }

        return request;
    }

    private sealed record MaterialResponse(Guid Id, decimal Quantity);

    [Fact]
    public async Task A_retried_adjustment_is_applied_once()
    {
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var materialId = await SeedMaterialAsync(client, 100m);
        var key = NewKey();

        var first = await client.SendAsync(Adjust(materialId, -40m, key));
        var second = await client.SendAsync(Adjust(materialId, -40m, key));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        // Sixty, not twenty. This single assertion is the whole feature.
        Assert.Equal(60m, await QuantityOf(materialId));
    }

    [Fact]
    public async Task A_replay_returns_the_first_answer_and_says_so()
    {
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var materialId = await SeedMaterialAsync(client, 100m);
        var key = NewKey();

        var first = await client.SendAsync(Adjust(materialId, -40m, key));
        var second = await client.SendAsync(Adjust(materialId, -40m, key));

        var firstBody = await first.Content.ReadFromJsonAsync<MaterialResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<MaterialResponse>();

        // The stored body, not a fresh read. If the retry recomputed the
        // answer it would report whatever the stock is now — which, after
        // somebody else's movement, is a number this request did not cause.
        Assert.Equal(firstBody!.Quantity, secondBody!.Quantity);

        Assert.False(first.Headers.Contains(IdempotencyFilter.ReplayHeaderName));
        Assert.True(second.Headers.Contains(IdempotencyFilter.ReplayHeaderName));
    }

    [Fact]
    public async Task Without_a_key_a_retry_applies_twice()
    {
        // Not a bug — the documented shape of the feature, pinned so that
        // "idempotency is on" can never be believed of a client that does not
        // send the header. It also states the cost of the opt-in design.
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var materialId = await SeedMaterialAsync(client, 100m);

        await client.SendAsync(Adjust(materialId, -40m, key: null));
        await client.SendAsync(Adjust(materialId, -40m, key: null));

        Assert.Equal(20m, await QuantityOf(materialId));
    }

    [Fact]
    public async Task The_same_key_on_a_different_request_is_refused()
    {
        // The client bug this catches: a key generated once when the screen
        // opened rather than once per action. Replaying the first answer would
        // silently drop the second movement and report success for it.
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var materialId = await SeedMaterialAsync(client, 100m);
        var key = NewKey();

        await client.SendAsync(Adjust(materialId, -40m, key));

        var different = await client.SendAsync(Adjust(materialId, -10m, key));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, different.StatusCode);

        // And the second movement was not applied under the guise of the first.
        Assert.Equal(60m, await QuantityOf(materialId));
    }

    [Fact]
    public async Task One_account_cannot_replay_another_accounts_response()
    {
        // Keys are chosen by clients, and two clients can pick the same one.
        // If the record were keyed on the key alone, the second caller would
        // be handed a body describing a material they may not be allowed to
        // see — and their own request would silently not happen.
        using var admin = _api.ClientAs(UserRole.SuperAdmin);
        using var manager = _api.ClientAs(UserRole.ProjectManager);

        var materialId = await SeedMaterialAsync(admin, 100m);
        var key = NewKey();

        var byAdmin = await admin.SendAsync(Adjust(materialId, -40m, key));
        var byManager = await manager.SendAsync(Adjust(materialId, -40m, key));

        Assert.Equal(HttpStatusCode.OK, byAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byManager.StatusCode);
        Assert.False(byManager.Headers.Contains(IdempotencyFilter.ReplayHeaderName));

        // Both were real requests from two different people, so both applied.
        Assert.Equal(20m, await QuantityOf(materialId));
    }

    [Fact]
    public async Task A_refused_request_leaves_the_key_free()
    {
        // Storing failures would hand the retry back the failure for ever —
        // including the transient one it was retrying to get past. Here the
        // first attempt is refused because the stock would go negative; the
        // corrected second attempt must be allowed to run.
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var materialId = await SeedMaterialAsync(client, 10m);
        var key = NewKey();

        var refused = await client.SendAsync(Adjust(materialId, -40m, key));

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        var accepted = await client.SendAsync(Adjust(materialId, -4m, key));

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(6m, await QuantityOf(materialId));
    }

    [Fact]
    public async Task Two_simultaneous_retries_apply_the_movement_once()
    {
        // The race the unique index exists for. A check-then-act store would
        // let both find nothing and both proceed, which is precisely the
        // double-apply the feature prevents — and precisely what a retry
        // fired while the first request is still in flight looks like.
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var materialId = await SeedMaterialAsync(client, 100m);
        var key = NewKey();

        var responses = await Task.WhenAll(
            client.SendAsync(Adjust(materialId, -40m, key)),
            client.SendAsync(Adjust(materialId, -40m, key)));

        Assert.Equal(60m, await QuantityOf(materialId));

        // One of them did the work; the other was told to wait or was handed
        // the answer. Which of the two depends on the timing, and both are
        // correct — the assertion that matters is the quantity above.
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses, r => Assert.Contains(
            r.StatusCode,
            new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task A_too_short_key_is_refused_rather_than_ignored()
    {
        // Ignoring it would be the dangerous failure: the client believes it
        // is protected, the server is not protecting it, and nothing says so
        // until a retry double-applies.
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var materialId = await SeedMaterialAsync(client, 100m);

        var response = await client.SendAsync(Adjust(materialId, -40m, key: "abc"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(100m, await QuantityOf(materialId));
    }

    [Fact]
    public async Task A_retried_assignment_replays_its_no_content_answer()
    {
        // An endpoint that returns 204 rather than a body: the filter has to
        // carry the status through with nothing to serialise.
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var employeeResponse = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            employeeNumber = $"IDEMP-{Guid.NewGuid():N}"[..20],
            firstName = "Ivan",
            lastName = "Horvat",
            position = "Zidar",
            employmentDate = "2024-01-15",
        });

        employeeResponse.EnsureSuccessStatusCode();

        var employee = await employeeResponse.Content.ReadFromJsonAsync<MaterialResponse>();

        var projectResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            name = $"Site {Guid.NewGuid():N}"[..20],
            client = "Test client",
            startDate = "2024-01-01",
        });

        projectResponse.EnsureSuccessStatusCode();

        var project = await projectResponse.Content.ReadFromJsonAsync<MaterialResponse>();

        var key = NewKey();
        var path = $"/api/v1/employees/{employee!.Id}/projects/{project!.Id}";

        var first = new HttpRequestMessage(HttpMethod.Post, path);
        first.Headers.Add(IdempotencyFilter.HeaderName, key);

        var second = new HttpRequestMessage(HttpMethod.Post, path);
        second.Headers.Add(IdempotencyFilter.HeaderName, key);

        var firstResponse = await client.SendAsync(first);
        var secondResponse = await client.SendAsync(second);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        // Without the key this second call answers 409 — the employee is
        // already on the project. That is the right answer to a second
        // assignment and the wrong answer to a retry of the first.
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);
        Assert.True(secondResponse.Headers.Contains(IdempotencyFilter.ReplayHeaderName));
    }
}
