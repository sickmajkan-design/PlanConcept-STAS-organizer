using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Application.Features.Authentication.Mfa;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The second factor over real HTTP: set it up, sign in with it, lose the
/// phone, and fail to guess it.
/// </summary>
[Collection(ApiCollection.Name)]
public class MfaFlowTests
{
    private readonly ApiFixture _api;

    public MfaFlowTests(ApiFixture api)
    {
        _api = api;
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static byte[] FromBase32(string text)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var buffer = 0;
        var bits = 0;

        foreach (var c in text.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(c);
            bits += 5;

            if (bits >= 8)
            {
                bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return bytes.ToArray();
    }

    private static string CodeAt(byte[] secret, int stepsFromNow) =>
        Totp.Compute(secret, Totp.StepOf(DateTime.UtcNow) + stepsFromNow);

    /// <summary>An Admin with the factor already switched on. Returns its address, secret and recovery codes.</summary>
    private async Task<(string Email, byte[] Secret, List<string> Recovery)> EnrolledAdminAsync()
    {
        var (email, _) = await _api.SeedSignInAccountAsync(UserRole.Admin);
        var token = await _api.SignInAsync(email);

        using var client = _api.AnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var setup = await client.PostAsync("/api/v1/auth/mfa/setup", null);
        setup.EnsureSuccessStatusCode();
        var setupBody = await setup.Content.ReadFromJsonAsync<JsonElement>(Json);
        var secret = FromBase32(setupBody.GetProperty("secret").GetString()!);

        var enable = await client.PostAsJsonAsync("/api/v1/auth/mfa/enable", new { code = CodeAt(secret, 0) });
        enable.EnsureSuccessStatusCode();
        var enabled = await enable.Content.ReadFromJsonAsync<JsonElement>(Json);

        var recovery = enabled.GetProperty("recoveryCodes").EnumerateArray()
            .Select(c => c.GetString()!)
            .ToList();

        return (email, secret, recovery);
    }

    private async Task<JsonElement> LoginAsync(string email)
    {
        using var client = _api.AnonymousClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = TestData.Password });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    private async Task<HttpResponseMessage> VerifyAsync(string mfaToken, string code)
    {
        using var client = _api.AnonymousClient();
        return await client.PostAsJsonAsync("/api/v1/auth/mfa/verify", new { mfaToken, code });
    }

    [Fact]
    public async Task Setup_gives_ten_recovery_codes_and_turns_the_factor_on()
    {
        var (_, _, recovery) = await EnrolledAdminAsync();

        Assert.Equal(10, recovery.Count);
        Assert.Equal(10, recovery.Distinct().Count());
        Assert.All(recovery, code => Assert.Matches("^[A-Z2-9]{5}-[A-Z2-9]{5}$", code));
    }

    [Fact]
    public async Task Sign_in_stops_at_a_challenge_and_issues_no_tokens()
    {
        var (email, _, _) = await EnrolledAdminAsync();

        var login = await LoginAsync(email);

        Assert.True(login.GetProperty("mfaRequired").GetBoolean());
        Assert.False(string.IsNullOrEmpty(login.GetProperty("mfaToken").GetString()));
        Assert.True(
            !login.TryGetProperty("accessToken", out var access) || access.ValueKind == JsonValueKind.Null,
            "No access token may be handed out before the code is given.");
    }

    [Fact]
    public async Task A_correct_code_completes_the_sign_in()
    {
        var (email, secret, _) = await EnrolledAdminAsync();
        var challenge = (await LoginAsync(email)).GetProperty("mfaToken").GetString()!;

        // Setup used the current step, so a code for the same step is a replay;
        // the next one is what the app shows half a minute later.
        var response = await VerifyAsync(challenge, CodeAt(secret, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.True(body.GetProperty("user").GetProperty("mfaEnabled").GetBoolean());
    }

    [Fact]
    public async Task A_code_that_was_just_used_cannot_be_used_again()
    {
        var (email, secret, _) = await EnrolledAdminAsync();
        var code = CodeAt(secret, 1);

        var first = await VerifyAsync((await LoginAsync(email)).GetProperty("mfaToken").GetString()!, code);
        var replay = await VerifyAsync((await LoginAsync(email)).GetProperty("mfaToken").GetString()!, code);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task A_wrong_code_is_refused()
    {
        var (email, secret, _) = await EnrolledAdminAsync();
        var challenge = (await LoginAsync(email)).GetProperty("mfaToken").GetString()!;
        var good = CodeAt(secret, 1);
        var wrong = good == "000000" ? "000001" : "000000";

        var response = await VerifyAsync(challenge, wrong);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Five_wrong_codes_spend_the_challenge_even_for_the_right_code()
    {
        var (email, secret, _) = await EnrolledAdminAsync();
        var challenge = (await LoginAsync(email)).GetProperty("mfaToken").GetString()!;
        var good = CodeAt(secret, 1);
        var wrong = good == "000000" ? "000001" : "000000";

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await VerifyAsync(challenge, wrong)).StatusCode);
        }

        var late = await VerifyAsync(challenge, good);

        Assert.Equal(HttpStatusCode.Unauthorized, late.StatusCode);
    }

    [Fact]
    public async Task A_recovery_code_works_once()
    {
        var (email, _, recovery) = await EnrolledAdminAsync();

        var first = await VerifyAsync((await LoginAsync(email)).GetProperty("mfaToken").GetString()!, recovery[0]);
        var again = await VerifyAsync((await LoginAsync(email)).GetProperty("mfaToken").GetString()!, recovery[0]);
        var other = await VerifyAsync((await LoginAsync(email)).GetProperty("mfaToken").GetString()!, recovery[1]);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, again.StatusCode);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    [Fact]
    public async Task A_made_up_challenge_is_refused()
    {
        var response = await VerifyAsync(new string('a', 64), "123456");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Only_a_super_admin_can_clear_somebody_elses_factor()
    {
        var (email, _, _) = await EnrolledAdminAsync();
        var target = await _api.InScope(async db =>
            db.Users.Single(u => u.Email == email).Id);

        var asAdmin = await _api.ClientAs(UserRole.Admin).PostAsync($"/api/v1/Users/{target}/mfa/reset", null);
        var asSuper = await _api.ClientAs(UserRole.SuperAdmin).PostAsync($"/api/v1/Users/{target}/mfa/reset", null);

        Assert.Equal(HttpStatusCode.Forbidden, asAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, asSuper.StatusCode);

        // And the next sign-in is plain again.
        var login = await LoginAsync(email);
        Assert.False(login.TryGetProperty("mfaRequired", out var required) && required.GetBoolean());
    }

    [Fact]
    public async Task Switching_it_off_needs_the_password_and_a_code()
    {
        var (email, secret, _) = await EnrolledAdminAsync();
        var verified = await VerifyAsync(
            (await LoginAsync(email)).GetProperty("mfaToken").GetString()!, CodeAt(secret, 1));
        var token = (await verified.Content.ReadFromJsonAsync<JsonElement>(Json))
            .GetProperty("accessToken").GetString()!;

        using var client = _api.AnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/v1/auth/mfa/disable", new { password = "NotThePassword1!", code = CodeAt(secret, 2) });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
    }
}
