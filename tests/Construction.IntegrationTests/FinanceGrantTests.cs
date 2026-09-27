using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Finance;
using Construction.Application.Features.Users.Commands.UpdateUser;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The customer's rule for figures in euro: only the Super Admin sees them, plus
/// whoever the Super Admin picks, and that pick is fixed so it can never fall
/// on a worker (or a customer login).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class FinanceGrantTests : IntegrationTestBase
{
    public FinanceGrantTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static UpdateUserCommand Grant(User target, FinanceAccess level) => new()
    {
        Id = target.Id,
        Email = target.Email,
        Role = target.Role,
        FinanceAccess = level,
    };

    [Fact]
    public async Task The_super_admin_can_grant_the_figures_to_a_foreman()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(Grant(foreman, FinanceAccess.Full));
        });

        var stored = await InScope(scope => scope.Db.Users
            .Where(u => u.Id == foreman.Id)
            .Select(u => u.FinanceAccess)
            .SingleAsync());

        Assert.Equal(FinanceAccess.Full, stored);
    }

    [Fact]
    public async Task The_figures_cannot_be_granted_to_a_worker()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker));

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(Grant(worker, FinanceAccess.Full));
        }));

        var stored = await InScope(scope => scope.Db.Users
            .Where(u => u.Id == worker.Id)
            .Select(u => u.FinanceAccess)
            .SingleAsync());

        Assert.Equal(FinanceAccess.None, stored);
    }

    [Theory]
    [InlineData(UserRole.Admin, FinanceAccess.None, false)]
    [InlineData(UserRole.Admin, FinanceAccess.StatisticsOnly, false)]
    [InlineData(UserRole.Admin, FinanceAccess.Full, true)]
    [InlineData(UserRole.ProjectManager, FinanceAccess.None, false)]
    [InlineData(UserRole.ProjectManager, FinanceAccess.Full, true)]
    [InlineData(UserRole.Foreman, FinanceAccess.Full, false)]
    [InlineData(UserRole.Worker, FinanceAccess.Full, false)]
    public async Task What_people_are_paid_is_shown_only_with_the_grant(
        UserRole role, FinanceAccess stored, bool expected)
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope, role));

        // Written straight to the row, so it also covers a value stored before the rule existed.
        await InScope(scope => scope.Db.Users
            .Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.FinanceAccess, stored)));

        var canSee = await InScope(scope =>
        {
            ActAs(scope, user);
            return FinanceRules.CanSeePayAsync(scope.Db, scope.CurrentUser, CancellationToken.None);
        });

        Assert.Equal(expected, canSee);
    }

    [Fact]
    public async Task The_super_admin_always_sees_what_people_are_paid()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var canSee = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return FinanceRules.CanSeePayAsync(scope.Db, scope.CurrentUser, CancellationToken.None);
        });

        Assert.True(canSee);
    }

    [Fact]
    public async Task Moving_an_account_to_worker_takes_the_grant_away()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(Grant(foreman, FinanceAccess.Full));
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new UpdateUserCommand
            {
                Id = foreman.Id,
                Email = foreman.Email,
                Role = UserRole.Worker,
            });
        });

        var stored = await InScope(scope => scope.Db.Users
            .Where(u => u.Id == foreman.Id)
            .Select(u => u.FinanceAccess)
            .SingleAsync());

        Assert.Equal(FinanceAccess.None, stored);
    }
}
