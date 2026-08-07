using System.Net;
using System.Text;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Every endpoint, against every role, over HTTP.
/// </summary>
/// <remarks>
/// <para>
/// The rest of the suite sets the current user directly and sends through
/// MediatR, so it proves what a handler does but never that the handler was
/// allowed to run. The <c>[Authorize]</c> attributes, the policy definitions
/// and the route table were asserted by nobody: an action that lost its
/// attribute in a merge, or a policy naming one role too many, would ship
/// green.
/// </para>
/// <para>
/// The trick that makes covering all of it affordable is that the request
/// bodies do not have to be valid. Authorization runs before model binding
/// reaches a handler, so a caller the policy admits gets 400 or 404 — never
/// 401 or 403. That means one empty body and one made-up id answer for every
/// endpoint, and the table below can stay a table.
/// </para>
/// </remarks>
[Collection(ApiCollection.Name)]
public class ApiAuthorizationTests
{
    private readonly ApiFixture _api;

    public ApiAuthorizationTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// One endpoint and the least senior role the policy on it admits.
    /// </summary>
    /// <param name="Minimum">
    /// Null when the endpoint is anonymous. Otherwise the junior end of the
    /// range: <see cref="UserRole"/> is declared most-senior-first, so a role
    /// is admitted exactly when its numeric value is at most this one. That is
    /// the same hierarchy the policies encode, stated once instead of five
    /// times.
    /// </param>
    /// <param name="HandlerNarrows">
    /// True where a rule inside the handler refuses some of the roles the
    /// policy admits — pay rates, mostly. Those get the 403 checked from below
    /// only: asserting that an admitted role is not refused would contradict a
    /// refusal the product intends.
    /// </param>
    public sealed record Endpoint(
        string Method,
        string Path,
        UserRole? Minimum,
        bool HandlerNarrows = false)
    {
        public override string ToString() => $"{Method} {Path}";
    }

    /// <summary>Stands in for a real id. Nothing is looked up before the policy runs.</summary>
    private const string Id = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    private const string OtherId = "17b1f2c3-4d5e-4f60-8a71-9b2c3d4e5f60";

    public static TheoryData<Endpoint> Endpoints()
    {
        var endpoints = new Endpoint[]
        {
            // ---- authentication ------------------------------------------
            // Every endpoint behind the credentials rate limiter is absent
            // from this table: login, forgot-password, reset-password,
            // change-password and users/{id}/password. The limiter runs before
            // authentication and allows twenty requests a minute across all
            // callers — a test server has no remote address, so everything
            // shares one partition. Six requests per endpoint would spend the
            // window and start answering 429 in place of the statuses under
            // test.
            //
            // That is not hypothetical. `users/{id}/password` was in this
            // table and passed for weeks, until adding a suite elsewhere
            // pushed the run past twenty and it began failing with 429 where
            // it expected 403 — a test whose result depended on how many other
            // tests had run first. Each of these now has its own case below,
            // costing one or two requests instead of six.
            new("POST", "/api/auth/refresh", null),
            new("POST", "/api/auth/logout", UserRole.Worker),
            new("GET", "/api/auth/me", UserRole.Worker),

            // ---- absences and the schedule board -------------------------
            new("GET", "/api/absences", UserRole.Worker),
            new("GET", "/api/schedule", UserRole.Worker),
            new("POST", "/api/absences", UserRole.Worker),
            new("POST", $"/api/absences/{Id}/review", UserRole.Foreman),
            new("DELETE", $"/api/absences/{Id}", UserRole.Worker),

            // ---- attachments ---------------------------------------------
            new("GET", "/api/attachments", UserRole.Worker),
            new("GET", "/api/attachments/expiring", UserRole.Admin),
            new("GET", $"/api/attachments/{Id}/content", UserRole.Worker),
            new("POST", "/api/attachments", UserRole.Worker),
            new("DELETE", $"/api/attachments/{Id}", UserRole.Admin),

            // ---- costs ----------------------------------------------------
            // The controller carries ForemanAndAbove; CostRules then narrows
            // pay rates to the people who may see somebody's pay.
            new("GET", "/api/employee-rates", UserRole.Foreman, HandlerNarrows: true),
            // Not flagged, though CostRules narrows this one too: the empty
            // body fails validation first, so the refusal never gets reached
            // and a 403 here would be a real regression. The flag marks what
            // is observably narrowed, not what is narrowed in principle.
            new("POST", "/api/employee-rates", UserRole.Foreman),
            new("DELETE", $"/api/employee-rates/{Id}", UserRole.Foreman, HandlerNarrows: true),
            new("GET", "/api/material-movements", UserRole.Foreman),
            new("POST", "/api/material-movements", UserRole.Foreman),
            new("DELETE", $"/api/material-movements/{Id}", UserRole.Foreman, HandlerNarrows: true),
            new("GET", "/api/vehicle-expenses", UserRole.Foreman),
            new("POST", "/api/vehicle-expenses", UserRole.Foreman),
            new("DELETE", $"/api/vehicle-expenses/{Id}", UserRole.Foreman, HandlerNarrows: true),
            new("GET", "/api/costs/projects", UserRole.Foreman),
            new("GET", "/api/costs/vehicles", UserRole.Foreman),

            // ---- employees ------------------------------------------------
            new("GET", "/api/employees", UserRole.Foreman),
            new("GET", $"/api/employees/{Id}", UserRole.Foreman),
            new("POST", "/api/employees", UserRole.Admin),
            new("PUT", $"/api/employees/{Id}", UserRole.Admin),
            new("DELETE", $"/api/employees/{Id}", UserRole.Admin),
            new("POST", $"/api/employees/{Id}/projects/{OtherId}", UserRole.ProjectManager),
            new("DELETE", $"/api/employees/{Id}/projects/{OtherId}", UserRole.ProjectManager),

            // ---- exports ---------------------------------------------------
            new("GET", "/api/exports/time-entries", UserRole.Foreman),
            new("GET", "/api/exports/project-costs", UserRole.Foreman),
            new("GET", "/api/exports/vehicle-costs", UserRole.Foreman),
            new("GET", "/api/exports/material-movements", UserRole.Foreman),

            // ---- locations --------------------------------------------------
            new("POST", "/api/locations", UserRole.Worker),
            new("GET", "/api/locations/current", UserRole.Foreman),
            new("GET", $"/api/locations/employees/{Id}/last", UserRole.Foreman),
            new("GET", $"/api/locations/employees/{Id}/history", UserRole.Foreman),

            // ---- materials --------------------------------------------------
            new("GET", "/api/materials", UserRole.Foreman),
            new("GET", $"/api/materials/{Id}", UserRole.Foreman),
            new("POST", "/api/materials", UserRole.ProjectManager),
            new("PUT", $"/api/materials/{Id}", UserRole.ProjectManager),
            new("POST", $"/api/materials/{Id}/adjust", UserRole.Foreman),
            new("DELETE", $"/api/materials/{Id}", UserRole.Admin),

            // ---- notifications ------------------------------------------------
            new("GET", "/api/notifications", UserRole.Worker),
            new("GET", "/api/notifications/unread-count", UserRole.Worker),
            new("POST", $"/api/notifications/{Id}/read", UserRole.Worker),
            new("POST", "/api/notifications/read-all", UserRole.Worker),
            new("POST", "/api/notifications/device-tokens", UserRole.Worker),
            new("POST", "/api/notifications/device-tokens/unregister", UserRole.Worker),
            new("POST", "/api/notifications/announce", UserRole.Admin),

            // ---- projects ---------------------------------------------------
            new("GET", "/api/projects", UserRole.Foreman),
            new("GET", $"/api/projects/{Id}", UserRole.Foreman),
            new("POST", "/api/projects", UserRole.ProjectManager),
            new("PUT", $"/api/projects/{Id}", UserRole.ProjectManager),
            new("DELETE", $"/api/projects/{Id}", UserRole.Admin),

            // ---- time entries -------------------------------------------------
            new("GET", "/api/timeentries", UserRole.Worker),
            new("GET", "/api/timeentries/summary", UserRole.Worker),
            new("GET", "/api/timeentries/current", UserRole.Worker),
            new("GET", $"/api/timeentries/{Id}", UserRole.Worker),
            new("POST", "/api/timeentries/clock-in", UserRole.Worker),
            new("POST", "/api/timeentries/clock-out", UserRole.Worker),
            new("POST", "/api/timeentries", UserRole.Foreman),
            new("PUT", $"/api/timeentries/{Id}", UserRole.Foreman),
            new("POST", $"/api/timeentries/{Id}/review", UserRole.ProjectManager),
            new("DELETE", $"/api/timeentries/{Id}", UserRole.Admin),

            // ---- tools -----------------------------------------------------
            new("GET", "/api/tools", UserRole.Foreman),
            new("GET", $"/api/tools/{Id}", UserRole.Foreman),
            // Open to everyone on purpose: a worker holding a tool has to be
            // able to scan it and find out what it is.
            new("GET", "/api/tools/by-qr/TOOL-0001", UserRole.Worker),
            new("POST", "/api/tools", UserRole.Admin),
            new("PUT", $"/api/tools/{Id}", UserRole.Admin),
            new("DELETE", $"/api/tools/{Id}", UserRole.Admin),
            new("POST", $"/api/tools/{Id}/assign-employee/{OtherId}", UserRole.Foreman),
            new("POST", $"/api/tools/{Id}/unassign-employee", UserRole.Foreman),
            new("POST", $"/api/tools/{Id}/assign-project/{OtherId}", UserRole.Foreman),
            new("POST", $"/api/tools/{Id}/unassign-project", UserRole.Foreman),

            // ---- user accounts ------------------------------------------------
            new("GET", "/api/users", UserRole.Admin),
            new("GET", $"/api/users/{Id}", UserRole.Admin),
            new("POST", "/api/users", UserRole.Admin),
            new("PUT", $"/api/users/{Id}", UserRole.Admin),
            new("POST", $"/api/users/{Id}/deactivate", UserRole.Admin),
            new("POST", $"/api/users/{Id}/activate", UserRole.Admin),

            // ---- vehicles ------------------------------------------------------
            new("GET", "/api/vehicles", UserRole.Foreman),
            new("GET", $"/api/vehicles/{Id}", UserRole.Foreman),
            new("POST", "/api/vehicles", UserRole.Admin),
            new("PUT", $"/api/vehicles/{Id}", UserRole.Admin),
            new("DELETE", $"/api/vehicles/{Id}", UserRole.Admin),
            new("POST", $"/api/vehicles/{Id}/assign/{OtherId}", UserRole.ProjectManager),
            new("POST", $"/api/vehicles/{Id}/unassign", UserRole.ProjectManager),

            // ---- work items -----------------------------------------------------
            new("GET", "/api/workitems", UserRole.Worker),
            new("GET", $"/api/workitems/{Id}", UserRole.Worker),
            // A worker may raise a defect; only a foreman may edit the record.
            new("POST", "/api/workitems", UserRole.Worker),
            new("PUT", $"/api/workitems/{Id}", UserRole.Foreman),
            new("POST", $"/api/workitems/{Id}/status", UserRole.Worker),
            new("DELETE", $"/api/workitems/{Id}", UserRole.Admin),
        };

        var data = new TheoryData<Endpoint>();

        foreach (var endpoint in endpoints)
        {
            data.Add(endpoint);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Every_role_gets_the_answer_its_policy_promises(Endpoint endpoint)
    {
        var failures = new List<string>();

        if (endpoint.Minimum is not null)
        {
            var anonymous = await SendAsync(_api.AnonymousClient(), endpoint);

            if (anonymous != HttpStatusCode.Unauthorized)
            {
                failures.Add($"anonymous got {(int)anonymous}, expected 401");
            }
        }

        foreach (var role in Enum.GetValues<UserRole>())
        {
            var status = await SendAsync(_api.ClientAs(role), endpoint);

            var admitted = endpoint.Minimum is null || (int)role <= (int)endpoint.Minimum;

            if (!admitted && status != HttpStatusCode.Forbidden)
            {
                failures.Add($"{role} got {(int)status}, expected 403");
            }

            // An admitted caller may still be told the body is wrong or the id
            // does not exist — that is the point of using neither. What it must
            // never be told is that it may not ask.
            if (admitted && status == HttpStatusCode.Unauthorized)
            {
                failures.Add($"{role} got 401 but the policy admits it");
            }

            if (admitted && !endpoint.HandlerNarrows && status == HttpStatusCode.Forbidden)
            {
                failures.Add($"{role} got 403 but the policy admits it");
            }
        }

        Assert.True(
            failures.Count == 0,
            $"{endpoint}:{Environment.NewLine} - {string.Join(Environment.NewLine + " - ", failures)}");
    }

    /// <summary>
    /// Setting another account's password is Admin and above.
    /// </summary>
    /// <remarks>
    /// Asked twice rather than six times, because it sits behind the
    /// credentials rate limiter. Two requests are enough: one role that must
    /// be refused, and one that must not.
    /// </remarks>
    [Fact]
    public async Task Setting_somebody_elses_password_is_for_administrators()
    {
        using var worker = _api.ClientAs(UserRole.Worker);

        var refused = await worker.PostAsync($"/api/users/{Id}/password", EmptyBody());

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);

        using var admin = _api.ClientAs(UserRole.Admin);

        var admitted = await admin.PostAsync($"/api/users/{Id}/password", EmptyBody());

        Assert.NotEqual(HttpStatusCode.Forbidden, admitted.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, admitted.StatusCode);
    }

    /// <summary>
    /// Change-password sits behind the credentials rate limiter, so it is
    /// asked once rather than six times.
    /// </summary>
    /// <remarks>
    /// The limiter runs before authentication in the pipeline. Put this
    /// endpoint in the table above and the twenty-per-minute window would be
    /// spent, after which every answer is 429 and the test proves nothing —
    /// including, quietly, the case where the endpoint had no
    /// <c>[Authorize]</c> at all.
    /// </remarks>
    [Fact]
    public async Task Changing_a_password_requires_signing_in_first()
    {
        using var client = _api.AnonymousClient();

        var response = await client.PostAsync("/api/auth/change-password", EmptyBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_by_somebody_else_is_not_a_token()
    {
        using var client = _api.AnonymousClient();

        // Structurally a JWT, signed with a key this API has never seen. The
        // shape is what makes it worth testing: a parser that validated the
        // claims before the signature would let this through.
        client.DefaultRequestHeaders.Add(
            "Authorization",
            "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" +
            ".eyJzdWIiOiIzZmE4NWY2NC01NzE3LTQ1NjItYjNmYy0yYzk2M2Y2NmFmYTYiLCJyb2xlIjoiU3VwZXJBZG1pbiJ9" +
            ".Ym9ndXMtc2lnbmF0dXJl");

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Records the window an offboarded account keeps: its access token stays
    /// valid until it expires.
    /// </summary>
    /// <remarks>
    /// This asserts what the system does, not what would be nicest. Nothing
    /// checks the account on each request — the bearer token is validated by
    /// signature and expiry alone — so deactivating somebody stops them
    /// refreshing but not using the token already in their hand, for up to the
    /// access-token lifetime (fifteen minutes as configured).
    ///
    /// That is a deliberate trade: a database lookup on every request buys
    /// immediate revocation at the cost of the property that makes a JWT worth
    /// having. Whether fifteen minutes is acceptable is a decision for whoever
    /// runs this, and it should be a decision rather than a surprise — which
    /// is why it is written down here as a test and not left to be discovered
    /// during an incident.
    /// </remarks>
    [Fact]
    public async Task A_deactivated_account_keeps_its_token_until_it_expires()
    {
        var (email, userId) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        var token = await _api.SignInAsync(email);

        using (var admin = _api.ClientAs(UserRole.SuperAdmin))
        {
            var deactivated = await admin.PostAsync(
                $"/api/users/{userId}/deactivate", EmptyBody());

            Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);
        }

        using var client = _api.AnonymousClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_health_check_is_open()
    {
        using var client = _api.AnonymousClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Sends one request and reports only its status. Takes ownership of the
    /// client, so the callers above can build one per role inline.
    /// </summary>
    private static async Task<HttpStatusCode> SendAsync(HttpClient client, Endpoint endpoint)
    {
        using (client)
        {
            using var request = new HttpRequestMessage(
                new HttpMethod(endpoint.Method), endpoint.Path);

            if (endpoint.Method is "POST" or "PUT")
            {
                request.Content = EmptyBody();
            }

            using var response = await client.SendAsync(request);

            return response.StatusCode;
        }
    }

    /// <summary>
    /// An empty object, which every command validator rejects.
    /// </summary>
    /// <remarks>
    /// Deliberately not a valid body. A valid one would have this suite
    /// creating and deleting real records as a side effect of asking who is
    /// allowed in, and each endpoint would need its own fixture — which is how
    /// authorization coverage ends up partial.
    /// </remarks>
    private static StringContent EmptyBody() =>
        new("{}", Encoding.UTF8, "application/json");
}
