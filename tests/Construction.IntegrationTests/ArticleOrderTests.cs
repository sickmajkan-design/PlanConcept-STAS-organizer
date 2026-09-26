using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Anyone may ask for articles; the office orders and sends them; the person who
/// asked confirms they arrived.
/// </summary>
[Collection(ApiCollection.Name)]
public class ArticleOrderTests
{
    private readonly ApiFixture _api;

    public ArticleOrderTests(ApiFixture api)
    {
        _api = api;
    }

    private static object Body(string name = "Radne cipele", object? project = null) => new
    {
        projectId = project,
        urgent = false,
        note = "Trebaju do ponedjeljka",
        items = new[] { new { name, quantity = 1, unit = "par", note = "broj 43" } },
    };

    private static async Task<JsonElement> AskAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/v1/articleorders", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<HttpResponseMessage> MoveAsync(HttpClient client, Guid id, string status, string? note = null) =>
        client.PostAsJsonAsync($"/api/v1/articleorders/{id}/status", new { status, note });

    private static async Task<HashSet<Guid>> IdsAsync(HttpClient client, string query = "")
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/v1/articleorders?pageSize=100{query}");

        return page.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToHashSet();
    }

    [Fact]
    public async Task Every_role_can_ask_and_starts_as_requested()
    {
        foreach (var role in Enum.GetValues<UserRole>().Where(r => r != UserRole.Customer))
        {
            using var client = _api.ClientAs(role);
            var order = await AskAsync(client, Body());

            Assert.Equal("Requested", order.GetProperty("status").GetString());
            Assert.Equal("Radne cipele", order.GetProperty("items")[0].GetProperty("name").GetString());
        }
    }

    [Fact]
    public async Task A_request_without_articles_or_with_zero_quantity_is_refused()
    {
        using var worker = _api.ClientAs(UserRole.Worker);

        var empty = await worker.PostAsJsonAsync("/api/v1/articleorders", new { items = Array.Empty<object>() });
        var zero = await worker.PostAsJsonAsync("/api/v1/articleorders", new { items = new[] { new { name = "Sljem", quantity = 0 } } });

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
    }

    [Fact]
    public async Task The_request_goes_ordered_then_in_delivery_then_the_requester_confirms()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(office, id, "Ordered")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(office, id, "InDelivery")).StatusCode);

        var done = await MoveAsync(worker, id, "Delivered");
        var body = await done.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.Equal("Delivered", body.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("deliveredAt").ValueKind);
    }

    [Fact]
    public async Task No_step_is_skipped()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Conflict, (await MoveAsync(office, id, "InDelivery")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await MoveAsync(worker, id, "Delivered")).StatusCode);
    }

    [Fact]
    public async Task Only_the_office_orders_and_only_the_requester_or_office_confirms_delivery()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var foreman = _api.ClientAs(UserRole.Foreman);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(worker, id, "Ordered")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(foreman, id, "Ordered")).StatusCode);

        await MoveAsync(office, id, "Ordered");
        await MoveAsync(office, id, "InDelivery");

        // Somebody who did not ask cannot say it arrived for them.
        Assert.Equal(HttpStatusCode.Forbidden, (await MoveAsync(foreman, id, "Delivered")).StatusCode);
    }

    [Fact]
    public async Task Declining_needs_a_reason_and_is_final()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await MoveAsync(office, id, "Rejected")).StatusCode);

        var declined = await MoveAsync(office, id, "Rejected", "Nije u budzetu");
        var body = await declined.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, declined.StatusCode);
        Assert.Equal("Nije u budzetu", body.GetProperty("reviewNote").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await MoveAsync(office, id, "Ordered")).StatusCode);
    }

    [Fact]
    public async Task The_requester_can_withdraw_only_before_it_is_ordered()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var office = _api.ClientAs(UserRole.Admin);
        var first = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();
        var second = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await MoveAsync(worker, first, "Cancelled")).StatusCode);

        await MoveAsync(office, second, "Ordered");
        Assert.Equal(HttpStatusCode.Conflict, (await MoveAsync(worker, second, "Cancelled")).StatusCode);
    }

    [Fact]
    public async Task A_worker_sees_only_their_own_and_the_office_sees_all()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var other = _api.ClientAs(UserRole.Foreman);
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        var mine = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();
        var theirs = (await AskAsync(other, Body("Sljem"))).GetProperty("id").GetGuid();

        var seenByWorker = await IdsAsync(worker);
        var seenByOffice = await IdsAsync(office);

        Assert.Contains(mine, seenByWorker);
        Assert.DoesNotContain(theirs, seenByWorker);
        Assert.Contains(mine, seenByOffice);
        Assert.Contains(theirs, seenByOffice);
    }

    [Fact]
    public async Task The_office_is_told_of_a_request_and_the_requester_of_each_step()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var office = _api.ClientAs(UserRole.Admin);
        var workerId = _api.UserIds[UserRole.Worker];
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        var asked = await _api.InScope(context => context.Notifications
            .CountAsync(n => n.Type == NotificationType.ArticleOrderRequested && n.UserId != workerId));

        Assert.True(asked > 0);

        await MoveAsync(office, id, "Ordered");

        var told = await _api.InScope(context => context.Notifications
            .CountAsync(n => n.Type == NotificationType.ArticleOrderStatusChanged && n.UserId == workerId));

        Assert.True(told > 0);
    }
}
