using Construction.Application.Features.SystemStatus;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The Super Admin sees failed sign-ins grouped by day, reason, address and account, and only accounts that exist are
/// named; a name nobody owns is counted, never listed.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LoginAttemptsTests : IntegrationTestBase
{
    public LoginAttemptsTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Failed_sign_ins_are_grouped_and_only_real_accounts_are_named()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var target = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker));
        var ip = $"203.0.113.{Random.Shared.Next(1, 250)}";
        var stranger = $"nobody-{Guid.NewGuid():N}@example.com";

        await InScope(async scope =>
        {
            var now = DateTime.UtcNow;
            scope.Db.FailedLogins.AddRange(
                new FailedLogin { UserId = target.Id, Email = target.Email, Reason = "WrongPassword", IpAddress = ip, OccurredAt = now.AddMinutes(-5) },
                new FailedLogin { UserId = target.Id, Email = target.Email, Reason = "WrongPassword", IpAddress = ip, OccurredAt = now.AddMinutes(-4) },
                new FailedLogin { UserId = null, Email = stranger, Reason = "UnknownAccount", IpAddress = ip, OccurredAt = now.AddMinutes(-3) },
                new FailedLogin { UserId = target.Id, Email = target.Email, Reason = "WrongPassword", IpAddress = "198.51.100.9", OccurredAt = now.AddDays(-20) });
            await scope.Db.SaveChangesAsync();
        });

        var result = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(owner.Id, owner.Role, null, owner.Email);
            return scope.Send(new GetLoginAttemptsQuery());
        });

        var row = Assert.Single(result.TopIps, i => i.Ip == ip);
        Assert.Equal(3, row.Count);
        Assert.Equal(2, row.Accounts);
        Assert.DoesNotContain(result.TopIps, i => i.Ip == "198.51.100.9");

        Assert.Contains(result.TopAccounts, a => a.Email == target.Email.ToLowerInvariant() && a.Count >= 2);
        Assert.DoesNotContain(result.TopAccounts, a => a.Email == stranger);
        Assert.Equal(7, result.Days.Count);
        Assert.True(result.Last24Hours >= 3);
    }

    [Fact]
    public async Task Only_the_super_admin_may_read_it()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);
            await Assert.ThrowsAnyAsync<Exception>(() => scope.Send(new GetLoginAttemptsQuery()));
        });
    }

    [Fact]
    public void The_fail2ban_file_is_read_and_a_missing_one_is_unknown()
    {
        var ssh = SshBansParser.Parse("banned_now=2\nbanned_total=15\nfailed_total=340\nips=1.2.3.4,5.6.7.8\n");

        Assert.True(ssh.Known);
        Assert.Equal(2, ssh.BannedNow);
        Assert.Equal(15, ssh.BannedTotal);
        Assert.Equal(340, ssh.FailedTotal);
        Assert.Equal(["1.2.3.4", "5.6.7.8"], ssh.BannedIps);

        Assert.False(SshBansParser.Parse(null).Known);
        Assert.False(SshBansParser.Parse("garbage").Known);
    }
}
