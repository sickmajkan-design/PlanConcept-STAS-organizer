using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Security;
using Construction.Application.Features.Authentication.Commands.Login;
using Construction.Application.Features.Authentication.Commands.Logout;
using Construction.Application.Features.Authentication.Commands.RefreshToken;
using Construction.Application.Features.Authentication.Models;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private Task<AuthResponse> LoginAsync(string email) =>
        InScope(scope => scope.Send(new LoginCommand
        {
            Email = email,
            Password = TestData.Password,
            IpAddress = "203.0.113.10"
        }));

    private Task<AuthResponse> RefreshAsync(string refreshToken) =>
        InScope(scope => scope.Send(new RefreshTokenCommand
        {
            RefreshToken = refreshToken,
            IpAddress = "203.0.113.10"
        }));

    private async Task LogoutAsync(string refreshToken)
    {
        using var scope = Fixture.CreateScope();
        await scope.Send(new LogoutCommand { RefreshToken = refreshToken });
    }

    [Fact]
    public async Task Login_issues_a_token_pair_for_valid_credentials()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var auth = await LoginAsync(user.Email);

        Assert.NotEmpty(auth.AccessToken);
        Assert.NotEmpty(auth.RefreshToken);
        Assert.Equal(user.Email, auth.User.Email);
        Assert.True(auth.AccessTokenExpiresAt > DateTime.UtcNow);
        Assert.True(auth.RefreshTokenExpiresAt > auth.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password_without_saying_which_part_was_wrong()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            InScope(scope => scope.Send(new LoginCommand
            {
                Email = user.Email,
                Password = "NotThePassword1!"
            })));

        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            InScope(scope => scope.Send(new LoginCommand
            {
                Email = "nobody@construction.test",
                Password = TestData.Password
            })));

        // Identical messages, so the endpoint cannot be used to probe which
        // addresses have accounts.
        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
    }

    [Fact]
    public async Task Login_refuses_a_deactivated_account()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope, isActive: false));

        await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(user.Email));
    }

    [Fact]
    public async Task Refresh_token_is_never_stored_in_the_clear()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var auth = await LoginAsync(user.Email);

        var stored = await InScope(scope => scope.Db.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .Select(rt => rt.TokenHash)
            .ToListAsync());

        Assert.DoesNotContain(auth.RefreshToken, stored);
        Assert.Contains(TokenHasher.Sha256(auth.RefreshToken), stored);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_records_what_replaced_it()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var first = await LoginAsync(user.Email);
        var second = await RefreshAsync(first.RefreshToken);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        var used = await InScope(scope => scope.Db.RefreshTokens
            .SingleAsync(rt => rt.TokenHash == TokenHasher.Sha256(first.RefreshToken)));

        Assert.NotNull(used.RevokedAt);
        Assert.Equal(TokenHasher.Sha256(second.RefreshToken), used.ReplacedByTokenHash);
        Assert.Equal("203.0.113.10", used.RevokedByIp);
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_every_session_the_user_has()
    {
        // The containment property: presenting an already-rotated token is
        // taken as evidence the token was stolen, so every live session for
        // that account is killed rather than only the presented one.
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var first = await LoginAsync(user.Email);
        var second = await RefreshAsync(first.RefreshToken);
        var otherDevice = await LoginAsync(user.Email);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(first.RefreshToken));

        // The token issued by the legitimate rotation is now dead too...
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(second.RefreshToken));
        // ...as is the unrelated session on another device.
        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(otherDevice.RefreshToken));

        var live = await InScope(scope => scope.Db.RefreshTokens
            .CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null));

        Assert.Equal(0, live);
    }

    [Fact]
    public async Task Refresh_rejects_a_token_that_has_expired()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));
        var auth = await LoginAsync(user.Email);

        using (var scope = Fixture.CreateScope())
        {
            scope.Clock.FreezeAt(DateTime.UtcNow.AddDays(8));
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                scope.Send(new RefreshTokenCommand { RefreshToken = auth.RefreshToken }));
        }

        using (var scope = Fixture.CreateScope())
        {
            scope.Clock.Reset();
        }
    }

    [Fact]
    public async Task Refresh_rejects_a_token_that_was_never_issued()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            RefreshAsync("a-token-that-was-never-issued"));
    }

    [Fact]
    public async Task Logout_revokes_the_presented_token_and_is_idempotent()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));
        var auth = await LoginAsync(user.Email);

        await LogoutAsync(auth.RefreshToken);

        // Repeating it must not throw — a client retrying a sign-out is normal.
        await LogoutAsync(auth.RefreshToken);

        var stored = await InScope(scope => scope.Db.RefreshTokens
            .SingleAsync(rt => rt.TokenHash == TokenHasher.Sha256(auth.RefreshToken)));

        Assert.NotNull(stored.RevokedAt);
    }

    [Fact]
    public async Task A_logged_out_token_cannot_be_refreshed()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));
        var auth = await LoginAsync(user.Email);

        await LogoutAsync(auth.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(auth.RefreshToken));
    }

    [Fact]
    public async Task Login_carries_the_employee_link_through_to_the_response()
    {
        var (user, employee) = await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var user = await TestData.SeedUserAsync(
                scope, UserRole.Worker, employeeId: employee.Id);
            return (user, employee);
        });

        var auth = await LoginAsync(user.Email);

        Assert.Equal(employee.Id, auth.User.EmployeeId);
        Assert.Equal(nameof(UserRole.Worker), auth.User.Role);
    }
}
