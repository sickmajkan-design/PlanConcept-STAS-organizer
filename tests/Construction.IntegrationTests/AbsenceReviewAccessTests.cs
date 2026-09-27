using System.Net;
using System.Net.Http.Json;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The customer's answer: only Super Admin and Admin grant or refuse leave.
/// A project manager or foreman sees requests but does not decide them.
/// </summary>
[Collection(ApiCollection.Name)]
public class AbsenceReviewAccessTests
{
    private readonly ApiFixture _api;

    public AbsenceReviewAccessTests(ApiFixture api)
    {
        _api = api;
    }

    private static Task<HttpResponseMessage> ReviewAsync(HttpClient client) =>
        client.PostAsJsonAsync($"/api/v1/absences/{Guid.NewGuid()}/review", new { approve = true });

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    [InlineData(UserRole.Worker)]
    public async Task Below_the_office_nobody_may_answer_a_request(UserRole role)
    {
        using var client = _api.ClientAs(role);

        Assert.Equal(HttpStatusCode.Forbidden, (await ReviewAsync(client)).StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.SuperAdmin)]
    public async Task Management_reaches_the_request_itself(UserRole role)
    {
        using var client = _api.ClientAs(role);

        // Past the policy, so the request is looked up — and this one does not exist.
        Assert.Equal(HttpStatusCode.NotFound, (await ReviewAsync(client)).StatusCode);
    }
}
