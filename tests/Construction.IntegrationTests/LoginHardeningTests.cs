using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Authentication.Commands.Login;
using Construction.Application.Features.Authentication.Commands.ResetPassword;
using Construction.Application.Features.Authentication.Models;
using Construction.Domain.Common;
using Construction.Domain.Entities;
using Construction.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Covers the brute-force and enumeration defences on sign-in. These are the
/// controls a password guesser meets first, so a regression here is not
/// visible in any feature test.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LoginHardeningTests : IntegrationTestBase
{
    public LoginHardeningTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private Task<AuthResponse> LoginAsync(string email, string password) =>
        InScope(scope => scope.Send(new LoginCommand
        {
            Email = email,
            Password = password,
            IpAddress = "203.0.113.10"
        }));

    private async Task FailLoginAsync(string email, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => LoginAsync(email, "WrongPassword1!"));
        }
    }

    private Task<User> ReloadAsync(Guid userId) =>
        InScope(scope => scope.Db.Users.AsNoTracking().SingleAsync(u => u.Id == userId));

    [Fact]
    public async Task Counts_consecutive_failures()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        await FailLoginAsync(user.Email, 3);

        Assert.Equal(3, (await ReloadAsync(user.Id)).FailedLoginAttempts);
    }

    [Fact]
    public async Task Locks_the_account_after_the_threshold()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        await FailLoginAsync(user.Email, LoginCommandHandler.MaxFailedAttempts);

        var locked = await ReloadAsync(user.Id);
        Assert.NotNull(locked.LockoutEndsAt);
        Assert.True(locked.IsLockedOut(DateTime.UtcNow));
    }

    [Fact]
    public async Task Refuses_the_correct_password_while_locked_out()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        await FailLoginAsync(user.Email, LoginCommandHandler.MaxFailedAttempts);

        // The whole point: guessing cannot continue even with the real
        // password, so the attacker's remaining guesses are worthless.
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => LoginAsync(user.Email, TestData.Password));
    }

    [Fact]
    public async Task Lockout_reports_the_same_message_as_a_wrong_password()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(
            () => LoginAsync(user.Email, "WrongPassword1!"));

        await FailLoginAsync(user.Email, LoginCommandHandler.MaxFailedAttempts);

        var locked = await Assert.ThrowsAsync<UnauthorizedException>(
            () => LoginAsync(user.Email, TestData.Password));

        // A distinct "account locked" message would confirm the address
        // exists, reopening the enumeration hole the dummy hash closes.
        Assert.Equal(wrongPassword.Message, locked.Message);
    }

    [Fact]
    public async Task Unknown_address_reports_the_same_message_as_a_wrong_password()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(
            () => LoginAsync(user.Email, "WrongPassword1!"));

        var unknown = await Assert.ThrowsAsync<UnauthorizedException>(
            () => LoginAsync("nobody-here@construction.test", "WrongPassword1!"));

        Assert.Equal(wrongPassword.Message, unknown.Message);
    }

    [Fact]
    public async Task A_successful_sign_in_clears_the_failure_count()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        await FailLoginAsync(user.Email, LoginCommandHandler.MaxFailedAttempts - 1);
        await LoginAsync(user.Email, TestData.Password);

        var reloaded = await ReloadAsync(user.Id);
        Assert.Equal(0, reloaded.FailedLoginAttempts);
        Assert.Null(reloaded.LockoutEndsAt);
    }

    [Fact]
    public async Task A_password_reset_clears_a_lockout()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));
        await FailLoginAsync(user.Email, LoginCommandHandler.MaxFailedAttempts);

        // Issue a reset token the way ForgotPassword does.
        const string rawToken = "reset-token-for-the-locked-out-user";

        await InScope(async scope =>
        {
            scope.Db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = Application.Common.Security.TokenHasher.Sha256(rawToken),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            await scope.Db.SaveChangesAsync();
        });

        await InScope(scope => scope.Send(new ResetPasswordCommand
        {
            Email = user.Email,
            Token = rawToken,
            NewPassword = "BrandNewPass1"
        }));

        // Otherwise an attacker could lock someone out and the recovery path
        // would not get them back in.
        var reloaded = await ReloadAsync(user.Id);
        Assert.Null(reloaded.LockoutEndsAt);

        await LoginAsync(user.Email, "BrandNewPass1");
    }

    [Fact]
    public void An_unknown_address_costs_a_full_password_derivation()
    {
        // The handler verifies against DummyHash when no account matched. If
        // this ever stopped being a real hash, sign-in would get fast again
        // for unknown addresses and become an enumeration oracle.
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("anything at all", hasher.DummyHash));
        Assert.Equal(hasher.DummyHash, hasher.DummyHash);
        Assert.StartsWith("100000.", hasher.DummyHash);
    }
}
