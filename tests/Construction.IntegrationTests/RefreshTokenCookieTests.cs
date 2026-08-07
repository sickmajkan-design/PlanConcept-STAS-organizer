using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.API.Authentication;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The refresh token as a browser receives it.
/// </summary>
/// <remarks>
/// <para>
/// The property under test is an absence: the token must not be in the
/// response body when a cookie was issued. A cookie that merely duplicates
/// what is already readable by script is not a mitigation — the attacker
/// reads the login response instead, and the vulnerability is exactly where it
/// was.
/// </para>
/// <para>
/// Over HTTP because the attributes are the whole point, and
/// <c>HttpOnly</c>/<c>SameSite</c> exist only on the wire.
/// </para>
/// </remarks>
[Collection(ApiCollection.Name)]
public class RefreshTokenCookieTests
{
    private readonly ApiFixture _api;

    public RefreshTokenCookieTests(ApiFixture api)
    {
        _api = api;
    }

    private static string? SetCookieHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith("construction.refresh="))
            : null;

    /// <summary>Signs in, asking for whichever delivery the test is about.</summary>
    private async Task<(HttpResponseMessage Response, JsonElement Body)> SignInAsync(
        HttpClient client,
        string email,
        bool wantCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = TestData.Password }),
        };

        if (wantCookie)
        {
            request.Headers.Add(RefreshTokenCookie.ModeHeader, RefreshTokenCookie.CookieMode);
        }

        var response = await client.SendAsync(request);

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        return (response, body);
    }

    [Fact]
    public async Task Asking_for_a_cookie_keeps_the_token_out_of_the_body()
    {
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var (response, body) = await SignInAsync(client, email, wantCookie: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The access token still comes back in the body: it is short-lived,
        // and the client needs it for the Authorization header on every call.
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));

        // The refresh token does not. This is the assertion the whole change
        // exists for.
        Assert.Equal(string.Empty, body.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task The_cookie_is_marked_so_script_cannot_read_it()
    {
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var (response, _) = await SignInAsync(client, email, wantCookie: true);

        var cookie = SetCookieHeader(response);

        Assert.NotNull(cookie);

        // Without this the cookie is just localStorage with extra steps.
        Assert.Contains("httponly", cookie!, StringComparison.OrdinalIgnoreCase);

        // Its own CSRF defence: another site cannot make the browser send it.
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        // Scoped, so it is not attached to every call the page makes.
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_client_that_does_not_ask_still_gets_the_token_in_the_body()
    {
        // The mobile app. It has platform secure storage and no cookie jar,
        // and changing what it receives would break it for no gain.
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var (response, body) = await SignInAsync(client, email, wantCookie: false);

        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("refreshToken").GetString()));
        Assert.Null(SetCookieHeader(response));
    }

    [Fact]
    public async Task A_browser_refreshes_with_the_cookie_and_an_empty_body()
    {
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var (loginResponse, _) = await SignInAsync(client, email, wantCookie: true);

        var cookieValue = SetCookieHeader(loginResponse)!.Split(';')[0];

        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { }),
        };

        refresh.Headers.Add("Cookie", cookieValue);
        refresh.Headers.Add(RefreshTokenCookie.ModeHeader, RefreshTokenCookie.CookieMode);

        var response = await client.SendAsync(refresh);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.Equal(string.Empty, body.GetProperty("refreshToken").GetString());

        // Rotated: a new cookie replaces the one just spent.
        var rotated = SetCookieHeader(response);
        Assert.NotNull(rotated);
        Assert.NotEqual(cookieValue, rotated!.Split(';')[0]);
    }

    [Fact]
    public async Task Refreshing_without_a_cookie_or_a_body_token_is_refused()
    {
        using var client = _api.ClientWithoutCookieJar();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { });

        // A validation failure, not a 500. The command requires a token and
        // there is nowhere left to find one.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Signing_out_removes_the_cookie()
    {
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var (loginResponse, loginBody) = await SignInAsync(client, email, wantCookie: true);

        var cookieValue = SetCookieHeader(loginResponse)!.Split(';')[0];

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new { }),
        };

        logout.Headers.Add("Cookie", cookieValue);
        logout.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", loginBody.GetProperty("accessToken").GetString());

        var response = await client.SendAsync(logout);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var cleared = SetCookieHeader(response);

        Assert.NotNull(cleared);

        // Expired in the past, which is how a cookie is deleted. The
        // attributes have to match the ones it was set with or the browser
        // treats this as a different cookie and keeps the original — a
        // sign-out that leaves the credential in place.
        Assert.Contains("expires=Thu, 01 Jan 1970", cleared!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cleared, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_spent_cookie_cannot_be_used_twice()
    {
        // Rotation and reuse detection are unchanged by where the token is
        // kept, which is the point of putting the cookie in front of the same
        // command rather than beside it.
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var (loginResponse, _) = await SignInAsync(client, email, wantCookie: true);

        var cookieValue = SetCookieHeader(loginResponse)!.Split(';')[0];

        async Task<HttpStatusCode> RefreshAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
            {
                Content = JsonContent.Create(new { }),
            };

            request.Headers.Add("Cookie", cookieValue);

            var response = await client.SendAsync(request);

            return response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.OK, await RefreshAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, await RefreshAsync());
    }

    [Fact]
    public async Task A_body_token_still_wins_over_a_stale_cookie()
    {
        // Body first, so a cookie left over from an earlier session cannot
        // quietly override a token the caller deliberately supplied.
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Worker);

        using var client = _api.ClientWithoutCookieJar();

        var (_, bodyLogin) = await SignInAsync(client, email, wantCookie: false);

        var bodyToken = bodyLogin.GetProperty("refreshToken").GetString();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = bodyToken }),
        };

        request.Headers.Add("Cookie", "construction.refresh=not-a-real-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
