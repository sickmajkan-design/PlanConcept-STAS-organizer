using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Asking to be paid back: the person asks with a reason and a receipt, the office decides,
/// nobody decides their own, and an approved one is tied to a payroll month.
/// </summary>
[Collection(ApiCollection.Name)]
public class RefundTests
{
    private readonly ApiFixture _api;

    public RefundTests(ApiFixture api)
    {
        _api = api;
    }

    private static object Body(decimal amount = 25.5m, string description = "Radne rukavice za gradiliste") => new
    {
        amount,
        currency = "EUR",
        expenseDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        description,
    };

    private static async Task<JsonElement> AskAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/v1/refunds", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<HttpResponseMessage> DecideAsync(HttpClient client, Guid id, string status, string? note = null, int? year = null, int? month = null) =>
        client.PostAsJsonAsync($"/api/v1/refunds/{id}/review", new { status, note, payrollYear = year, payrollMonth = month });

    private static async Task<HashSet<Guid>> IdsAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<JsonElement>("/api/v1/refunds?pageSize=100");

        return page.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToHashSet();
    }

    [Fact]
    public async Task A_worker_asks_and_it_waits_for_a_decision()
    {
        using var worker = _api.ClientAs(UserRole.Worker);

        var refund = await AskAsync(worker, Body());

        Assert.Equal("Requested", refund.GetProperty("status").GetString());
        Assert.Equal(25.5m, refund.GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task A_request_needs_a_reason_and_a_positive_amount()
    {
        using var worker = _api.ClientAs(UserRole.Worker);

        Assert.Equal(HttpStatusCode.BadRequest, (await worker.PostAsJsonAsync("/api/v1/refunds", Body(description: ""))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await worker.PostAsJsonAsync("/api/v1/refunds", Body(amount: 0))).StatusCode);
    }

    [Fact]
    public async Task The_office_approves_into_a_payroll_month_and_the_person_is_told()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        var approved = await DecideAsync(office, id, "Approved", year: 2026, month: 10);
        var body = await approved.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal("Approved", body.GetProperty("status").GetString());
        Assert.Equal(2026, body.GetProperty("payrollYear").GetInt32());
        Assert.Equal(10, body.GetProperty("payrollMonth").GetInt32());
    }

    [Fact]
    public async Task Declining_needs_a_reason_and_a_decided_request_cannot_be_decided_again()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await DecideAsync(office, id, "Rejected")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DecideAsync(office, id, "Rejected", "Nema racuna")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await DecideAsync(office, id, "Approved")).StatusCode);
    }

    [Fact]
    public async Task Nobody_decides_their_own_request_and_a_worker_decides_nothing()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var manager = _api.ClientAs(UserRole.ProjectManager);
        var own = (await AskAsync(manager, Body())).GetProperty("id").GetGuid();
        var theirs = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await DecideAsync(manager, own, "Approved")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await DecideAsync(worker, theirs, "Approved")).StatusCode);
    }

    [Fact]
    public async Task The_person_can_withdraw_their_own_undecided_request_and_nobody_elses()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var foreman = _api.ClientAs(UserRole.Foreman);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await DecideAsync(foreman, id, "Cancelled")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DecideAsync(worker, id, "Cancelled")).StatusCode);
    }

    [Fact]
    public async Task Money_is_private_a_worker_sees_only_their_own_and_the_office_sees_all()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var foreman = _api.ClientAs(UserRole.Foreman);
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        var mine = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();
        var theirs = (await AskAsync(foreman, Body())).GetProperty("id").GetGuid();

        var seenByWorker = await IdsAsync(worker);
        var seenByForeman = await IdsAsync(foreman);
        var seenByOffice = await IdsAsync(office);

        Assert.Contains(mine, seenByWorker);
        Assert.DoesNotContain(theirs, seenByWorker);
        Assert.DoesNotContain(mine, seenByForeman);
        Assert.Contains(mine, seenByOffice);
        Assert.Contains(theirs, seenByOffice);
    }

    private static MultipartFormDataContent Receipt(string ownerId)
    {
        var file = new ByteArrayContent([1, 2, 3, 4]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        return new MultipartFormDataContent
        {
            { file, "file", "racun.jpg" },
            { new StringContent("Refund"), "ownerType" },
            { new StringContent(ownerId), "ownerId" },
            { new StringContent("Photo"), "category" },
        };
    }

    [Fact]
    public async Task The_person_attaches_a_receipt_to_their_own_request_and_others_cannot()
    {
        using var worker = _api.ClientAs(UserRole.Worker);
        using var foreman = _api.ClientAs(UserRole.Foreman);
        var id = (await AskAsync(worker, Body())).GetProperty("id").GetGuid();

        var mine = await worker.PostAsync("/api/v1/attachments", Receipt(id.ToString()));
        var notTheirs = await foreman.PostAsync("/api/v1/attachments", Receipt(id.ToString()));

        Assert.Equal(HttpStatusCode.Created, mine.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, notTheirs.StatusCode);

        var listed = await worker.GetFromJsonAsync<JsonElement>($"/api/v1/attachments?ownerType=Refund&ownerId={id}");
        Assert.Equal(1, listed.GetArrayLength());

        var refused = await foreman.GetAsync($"/api/v1/attachments?ownerType=Refund&ownerId={id}");
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
    }
}
