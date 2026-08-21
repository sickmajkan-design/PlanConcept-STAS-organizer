using System.Net;
using System.Net.Http.Json;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Putting somebody on a site without saying when they come off.
/// </summary>
/// <remarks>
/// Postings gained optional start and end dates, and the parameter carrying
/// them became a required body — which MVC enforces before a handler is ever
/// reached, with 415, for a POST that has no body at all. That is exactly what
/// the employee detail page sends: it assigns somebody to a site with no end
/// in mind and no dates to give. The screen simply stopped working, and the
/// only thing that noticed was an idempotency test asserting an unrelated
/// point about replayed answers.
///
/// This is at the HTTP level on purpose. The rule lives in model binding, so a
/// test that sends the command straight to MediatR — as the schedule tests do,
/// correctly, for the rules themselves — cannot see it.
/// </remarks>
[Collection(ApiCollection.Name)]
public class AssignmentBodyTests
{
    private readonly ApiFixture _api;

    public AssignmentBodyTests(ApiFixture api)
    {
        _api = api;
    }

    private sealed record Created(Guid Id);

    private static async Task<Guid> SeedEmployeeAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            employeeNumber = $"ASSIGN-{Guid.NewGuid():N}"[..20],
            firstName = "Ivan",
            lastName = "Horvat",
            position = "Zidar",
            employmentDate = "2024-01-15",
        });

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<Created>())!.Id;
    }

    private static async Task<Guid> SeedProjectAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            name = $"Site {Guid.NewGuid():N}"[..20],
            client = "Test client",
            startDate = "2024-01-01",
        });

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<Created>())!.Id;
    }

    [Fact]
    public async Task Assigning_with_no_body_at_all_is_accepted()
    {
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var employeeId = await SeedEmployeeAsync(client);
        var projectId = await SeedProjectAsync(client);

        // No body, no Content-Type — the request the office makes when it puts
        // somebody on a site until further notice.
        var response = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/employees/{employeeId}/projects/{projectId}"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Assigning_with_dates_is_still_accepted()
    {
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var employeeId = await SeedEmployeeAsync(client);
        var projectId = await SeedProjectAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employeeId}/projects/{projectId}",
            new { startDate = "2026-03-02", endDate = "2026-03-06" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_posting_that_ends_before_it_starts_is_still_refused()
    {
        // The body being optional must not make what is in it optional too.
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        var employeeId = await SeedEmployeeAsync(client);
        var projectId = await SeedProjectAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/employees/{employeeId}/projects/{projectId}",
            new { startDate = "2026-03-06", endDate = "2026-03-02" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
