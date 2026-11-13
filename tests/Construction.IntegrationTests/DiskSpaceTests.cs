using Construction.Application.Features.Attention;
using Construction.Application.Features.SystemStatus;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The disk is watched: the Super Admin is told once a day while it is low, sees it on the home list, and can read the
/// numbers at any time. Admins and below are not told, because they cannot do anything about it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class DiskSpaceTests : IntegrationTestBase
{
    public DiskSpaceTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private Task SetFreeAsync(double freeGigabytes) =>
        InScope(scope =>
        {
            scope.Disk.SetFree(freeGigabytes);
            return Task.CompletedTask;
        });

    private Task<List<string>> AttentionKeysAsync(User user) =>
        InScope(async scope =>
        {
            ActAs(scope, user);
            var attention = await scope.Send(new GetAttentionQuery());

            return attention.Groups.Select(g => g.Key).ToList();
        });

    private Task<int> NoticesAsync(Guid userId) =>
        InScope(scope => scope.Db.Notifications.CountAsync(n =>
            n.UserId == userId
            && (n.Title == SendDiskSpaceWarningCommandHandler.LowTitle
                || n.Title == SendDiskSpaceWarningCommandHandler.CriticalTitle)));

    private async Task WithDiskAsync(double freeGigabytes, Func<Task> test)
    {
        await SetFreeAsync(freeGigabytes);

        try
        {
            await test();
        }
        finally
        {
            await InScope(scope =>
            {
                scope.Disk.Reset();
                return Task.CompletedTask;
            });
        }
    }

    [Fact]
    public async Task Nothing_is_sent_while_there_is_room()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        await WithDiskAsync(30, async () =>
        {
            var sent = await InScope(scope => scope.Send(new SendDiskSpaceWarningCommand()));

            Assert.Equal(0, sent);
            Assert.Equal(0, await NoticesAsync(owner.Id));
            Assert.DoesNotContain("diskSpace", await AttentionKeysAsync(owner));
        });
    }

    [Fact]
    public async Task A_low_disk_tells_the_super_admin_once_and_not_the_admin()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        // 6 GB of 40 is 15%: under the 20% line, over the 10% one.
        await WithDiskAsync(6, async () =>
        {
            await InScope(scope => scope.Send(new SendDiskSpaceWarningCommand()));
            await InScope(scope => scope.Send(new SendDiskSpaceWarningCommand()));

            Assert.Equal(1, await NoticesAsync(owner.Id));
            Assert.Equal(0, await NoticesAsync(admin.Id));

            var notice = await InScope(scope => scope.Db.Notifications.SingleAsync(n =>
                n.UserId == owner.Id && n.Title == SendDiskSpaceWarningCommandHandler.LowTitle));
            Assert.Contains("6.0 GB / 40.0 GB (15%)", notice.Body);
        });
    }

    [Fact]
    public async Task A_disk_close_to_out_says_so()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        await WithDiskAsync(2, async () =>
        {
            await InScope(scope => scope.Send(new SendDiskSpaceWarningCommand()));

            Assert.Equal(1, await InScope(scope => scope.Db.Notifications.CountAsync(n =>
                n.UserId == owner.Id && n.Title == SendDiskSpaceWarningCommandHandler.CriticalTitle)));
        });
    }

    [Fact]
    public async Task The_home_list_shows_it_to_the_super_admin_only()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await WithDiskAsync(6, async () =>
        {
            Assert.Contains("diskSpace", await AttentionKeysAsync(owner));
            Assert.DoesNotContain("diskSpace", await AttentionKeysAsync(admin));
        });

        await WithDiskAsync(2, async () =>
        {
            Assert.Contains("diskSpaceCritical", await AttentionKeysAsync(owner));
            Assert.DoesNotContain("diskSpaceCritical", await AttentionKeysAsync(admin));
        });
    }

    [Fact]
    public async Task The_numbers_can_be_read_at_any_time()
    {
        await WithDiskAsync(6, async () =>
        {
            var status = await InScope(scope => scope.Send(new GetStorageStatusQuery()));

            Assert.True(status.Known);
            Assert.Equal(15.0, status.FreePercent);
            Assert.Equal(1, status.Level);
            Assert.Equal(status.TotalBytes - status.FreeBytes, status.UsedBytes);
        });
    }

    [Fact]
    public async Task A_disk_that_cannot_be_read_never_breaks_the_page()
    {
        await InScope(scope =>
        {
            scope.Disk.Reading = null;
            return Task.CompletedTask;
        });

        try
        {
            var status = await InScope(scope => scope.Send(new GetStorageStatusQuery()));
            var sent = await InScope(scope => scope.Send(new SendDiskSpaceWarningCommand()));

            Assert.False(status.Known);
            Assert.Equal(0, sent);
        }
        finally
        {
            await InScope(scope =>
            {
                scope.Disk.Reset();
                return Task.CompletedTask;
            });
        }
    }
}
