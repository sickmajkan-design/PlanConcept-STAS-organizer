using System.Net;
using System.Net.Http.Headers;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A token that is still cryptographically valid stops working the moment the
/// account behind it changes — not fifteen minutes later.
/// </summary>
[Collection(ApiCollection.Name)]
public class TokenAccountValidationTests
{
    private readonly ApiFixture _api;

    public TokenAccountValidationTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<(HttpClient Client, Guid UserId)> SignedInAsync(UserRole role)
    {
        var (email, userId) = await _api.SeedSignInAccountAsync(role);
        var token = await _api.SignInAsync(email);

        var client = _api.AnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (client, userId);
    }

    [Fact]
    public async Task An_unchanged_account_keeps_working()
    {
        var (client, _) = await SignedInAsync(UserRole.Admin);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task A_demotion_takes_effect_on_the_next_request()
    {
        var (client, userId) = await SignedInAsync(UserRole.Admin);

        await _api.InScope(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == userId);
            user.Role = UserRole.Foreman;
            await db.SaveChangesAsync();
            return 0;
        });

        // The token still says Admin and has not expired. It must not be
        // believed.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task A_deactivated_account_is_locked_out_at_once()
    {
        var (client, userId) = await SignedInAsync(UserRole.Foreman);

        await _api.InScope(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == userId);
            user.IsActive = false;
            await db.SaveChangesAsync();
            return 0;
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Unlinking_an_employee_invalidates_the_token_that_named_them()
    {
        // The employee id in the token decides whose timesheet "mine" means.
        var (client, userId) = await SignedInAsync(UserRole.Worker);

        await _api.InScope(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == userId);
            user.EmployeeId = null;
            await db.SaveChangesAsync();
            return 0;
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task A_token_for_an_account_that_was_deleted_is_refused()
    {
        var (client, userId) = await SignedInAsync(UserRole.Admin);

        await _api.InScope(async db =>
        {
            await db.RefreshTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync();
            await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();
            return 0;
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }
}
