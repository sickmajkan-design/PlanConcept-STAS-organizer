using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Security;
using Construction.Application.Features.Authentication.Commands.Login;
using Construction.Application.Features.Authentication.Commands.RefreshToken;
using Construction.Application.Features.Users.Commands.ActivateUser;
using Construction.Application.Features.Users.Commands.CreateUser;
using Construction.Application.Features.Users.Commands.DeactivateUser;
using Construction.Application.Features.Users.Commands.SetUserPassword;
using Construction.Application.Features.Users.Commands.UpdateUser;
using Construction.Application.Features.Users.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Account administration, and in particular whether offboarding actually
/// takes access away rather than only setting a flag.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class UserManagementTests : IntegrationTestBase
{
    public UserManagementTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private Task<T> AsRole<T>(UserRole role, Func<TestScope, Task<T>> action, Guid? actingUserId = null) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(actingUserId ?? Guid.NewGuid(), role);
            return action(scope);
        });

    private Task AsRole(UserRole role, Func<TestScope, Task> action, Guid? actingUserId = null) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(actingUserId ?? Guid.NewGuid(), role);
            return action(scope);
        });

    private static CreateUserCommand NewUser(UserRole role, Guid? employeeId = null) => new()
    {
        Email = $"user-{Guid.NewGuid():N}@construction.test",
        Password = "Onboard1234",
        Role = role,
        EmployeeId = employeeId
    };

    // --- Offboarding ------------------------------------------------------

    [Fact]
    public async Task Deactivating_stops_the_account_signing_in()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker)));

        await AsRole(UserRole.Admin, s => s.Send(new DeactivateUserCommand(created.Id)));

        await Assert.ThrowsAsync<UnauthorizedException>(() => InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Onboard1234"
        })));
    }

    [Fact]
    public async Task Deactivating_kills_a_session_that_is_already_open()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker)));

        var session = await InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Onboard1234"
        }));

        await AsRole(UserRole.Admin, s => s.Send(new DeactivateUserCommand(created.Id)));

        // The refresh token is the part that outlives the 15-minute access
        // token. If this still worked, "offboarded" would mean nothing for
        // another seven days.
        await Assert.ThrowsAsync<UnauthorizedException>(() => InScope(s => s.Send(
            new RefreshTokenCommand { RefreshToken = session.RefreshToken })));

        var stored = await InScope(s => s.Db.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.UserId == created.Id)
            .ToListAsync());

        Assert.All(stored, token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task Deactivating_removes_device_registrations()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker)));

        await InScope(async s =>
        {
            s.Db.DeviceTokens.Add(new DeviceToken
            {
                UserId = created.Id,
                Token = $"fcm-{Guid.NewGuid():N}",
                Platform = DevicePlatform.Android,
                LastUsedAt = DateTime.UtcNow
            });

            await s.Db.SaveChangesAsync();
        });

        await AsRole(UserRole.Admin, s => s.Send(new DeactivateUserCommand(created.Id)));

        // Push is delivered to a device, not through an access check, so a
        // leftover registration keeps sending project notifications to
        // someone who no longer works here.
        var remaining = await InScope(s => s.Db.DeviceTokens
            .AsNoTracking()
            .CountAsync(dt => dt.UserId == created.Id));

        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task Deactivating_invalidates_an_outstanding_reset_link()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker)));

        await InScope(async s =>
        {
            s.Db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = created.Id,
                TokenHash = TokenHasher.Sha256("a-link-already-in-their-inbox"),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

            await s.Db.SaveChangesAsync();
        });

        await AsRole(UserRole.Admin, s => s.Send(new DeactivateUserCommand(created.Id)));

        var outstanding = await InScope(s => s.Db.PasswordResetTokens
            .AsNoTracking()
            .CountAsync(t => t.UserId == created.Id && t.UsedAt == null));

        Assert.Equal(0, outstanding);
    }

    [Fact]
    public async Task Reactivating_restores_sign_in_but_not_the_old_sessions()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker)));

        var session = await InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Onboard1234"
        }));

        await AsRole(UserRole.Admin, s => s.Send(new DeactivateUserCommand(created.Id)));
        await AsRole(UserRole.Admin, s => s.Send(new ActivateUserCommand(created.Id)));

        await InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Onboard1234"
        }));

        await Assert.ThrowsAsync<UnauthorizedException>(() => InScope(s => s.Send(
            new RefreshTokenCommand { RefreshToken = session.RefreshToken })));
    }

    // --- Privilege escalation guards ---------------------------------------

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.Admin)]
    public async Task An_admin_cannot_create_an_account_at_or_above_their_own_role(UserRole role)
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => AsRole(UserRole.Admin, s => s.Send(NewUser(role))));
    }

    [Fact]
    public async Task A_super_admin_can_create_an_admin()
    {
        var created = await AsRole(UserRole.SuperAdmin, s => s.Send(NewUser(UserRole.Admin)));

        Assert.Equal(nameof(UserRole.Admin), created.Role);
    }

    [Fact]
    public async Task An_admin_cannot_promote_someone_above_their_own_role()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker)));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => AsRole(UserRole.Admin, s => s.Send(new UpdateUserCommand
            {
                Id = created.Id,
                Email = created.Email,
                Role = UserRole.SuperAdmin
            })));
    }

    [Fact]
    public async Task An_admin_cannot_touch_a_peer_account()
    {
        var peer = await AsRole(UserRole.SuperAdmin, s => s.Send(NewUser(UserRole.Admin)));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => AsRole(UserRole.Admin, s => s.Send(new DeactivateUserCommand(peer.Id))));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => AsRole(UserRole.Admin, s => s.Send(new SetUserPasswordCommand
            {
                Id = peer.Id,
                NewPassword = "TakenOver1"
            })));
    }

    // --- Lockout protections ----------------------------------------------

    [Fact]
    public async Task Nobody_can_deactivate_their_own_account()
    {
        var created = await AsRole(UserRole.SuperAdmin, s => s.Send(NewUser(UserRole.Admin)));

        await Assert.ThrowsAsync<ConflictException>(() => AsRole(
            UserRole.Admin,
            s => s.Send(new DeactivateUserCommand(created.Id)),
            actingUserId: created.Id));
    }

    [Fact]
    public async Task The_last_active_super_admin_cannot_be_deactivated()
    {
        // Whatever else the database holds, at the moment of the call there
        // must be another active Super Admin or the system becomes
        // unadministrable except through direct database access.
        var onlyOne = await InScope(async s =>
            !await s.Db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin && u.IsActive));

        var superAdmin = await AsRole(UserRole.SuperAdmin, s => s.Send(NewUser(UserRole.SuperAdmin)));

        var others = await InScope(s => s.Db.Users
            .CountAsync(u => u.Role == UserRole.SuperAdmin && u.IsActive && u.Id != superAdmin.Id));

        if (others == 0 || onlyOne)
        {
            await Assert.ThrowsAsync<ConflictException>(
                () => AsRole(UserRole.SuperAdmin, s => s.Send(new DeactivateUserCommand(superAdmin.Id))));
        }
        else
        {
            // Another one exists, so removing this one is allowed.
            await AsRole(UserRole.SuperAdmin, s => s.Send(new DeactivateUserCommand(superAdmin.Id)));
        }
    }

    // --- Account creation --------------------------------------------------

    [Fact]
    public async Task An_email_can_only_be_used_once()
    {
        var command = NewUser(UserRole.Worker);
        await AsRole(UserRole.Admin, s => s.Send(command));

        await Assert.ThrowsAsync<ConflictException>(
            () => AsRole(UserRole.Admin, s => s.Send(command)));
    }

    [Fact]
    public async Task An_employee_can_only_hold_one_account()
    {
        var employee = await InScope(s => TestData.SeedEmployeeAsync(s));

        await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker, employee.Id)));

        await Assert.ThrowsAsync<ConflictException>(
            () => AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker, employee.Id))));
    }

    [Fact]
    public async Task A_new_account_can_sign_in_with_the_password_it_was_given()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Foreman)));

        var session = await InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Onboard1234"
        }));

        Assert.NotEmpty(session.AccessToken);
    }

    // --- Role change --------------------------------------------------------

    [Fact]
    public async Task Changing_a_role_ends_the_sessions_carrying_the_old_one()
    {
        var created = await AsRole(UserRole.SuperAdmin, s => s.Send(NewUser(UserRole.ProjectManager)));

        var session = await InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Onboard1234"
        }));

        await AsRole(UserRole.SuperAdmin, s => s.Send(new UpdateUserCommand
        {
            Id = created.Id,
            Email = created.Email,
            Role = UserRole.Worker
        }));

        // A demotion that left the old sessions running would keep the old
        // permissions alive inside already-issued tokens.
        await Assert.ThrowsAsync<UnauthorizedException>(() => InScope(s => s.Send(
            new RefreshTokenCommand { RefreshToken = session.RefreshToken })));
    }

    // --- Administrator-set password ----------------------------------------

    [Fact]
    public async Task An_admin_set_password_works_and_ends_existing_sessions()
    {
        var created = await AsRole(UserRole.Admin, s => s.Send(NewUser(UserRole.Worker)));

        var session = await InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Onboard1234"
        }));

        await AsRole(UserRole.Admin, s => s.Send(new SetUserPasswordCommand
        {
            Id = created.Id,
            NewPassword = "Replaced9876"
        }));

        await Assert.ThrowsAsync<UnauthorizedException>(() => InScope(s => s.Send(
            new RefreshTokenCommand { RefreshToken = session.RefreshToken })));

        await InScope(s => s.Send(new LoginCommand
        {
            Email = created.Email,
            Password = "Replaced9876"
        }));
    }

    [Fact]
    public async Task The_password_endpoint_refuses_to_change_your_own()
    {
        var created = await AsRole(UserRole.SuperAdmin, s => s.Send(NewUser(UserRole.Admin)));

        // Changing your own password must go through the flow that asks for
        // the current one; this endpoint does not.
        await Assert.ThrowsAsync<ConflictException>(() => AsRole(
            UserRole.Admin,
            s => s.Send(new SetUserPasswordCommand { Id = created.Id, NewPassword = "SelfServe11" }),
            actingUserId: created.Id));
    }
}
