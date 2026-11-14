using Construction.Application.Features.SystemStatus;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Unexpected errors are counted in memory, saved once a minute with no message in them, listed for the Super Admin by
/// kind, and removed after 30 days.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ServerErrorTests : IntegrationTestBase
{
    public ServerErrorTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static Exception Thrown(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Fact]
    public async Task Errors_are_saved_by_kind_without_their_message_and_listed_for_the_super_admin()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var route = $"GET /api/test/{Guid.NewGuid():N}/{{id}}";

        await InScope(async scope =>
        {
            var buffer = scope.Get<ServerErrorBuffer>();
            buffer.Record(Thrown("secret@example.com"), route, "9.9.9", DateTime.UtcNow);
            buffer.Record(Thrown("other"), route, "9.9.9", DateTime.UtcNow);
            await scope.Send(new FlushServerErrorsCommand());
        });

        var list = await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(owner.Id, owner.Role, null, owner.Email);
            return await scope.Send(new GetServerErrorsQuery());
        });

        var group = Assert.Single(list, g => g.Route == route);
        Assert.Equal(2, group.Total);
        Assert.Equal(2, group.Today);
        Assert.Equal("9.9.9", group.FirstVersion);

        var stored = await InScope(scope => scope.Db.ServerErrors.Where(e => e.Route == route).ToListAsync());
        Assert.DoesNotContain("secret@example.com", string.Join("|", stored.Select(e => $"{e.ExceptionType}{e.Location}{e.Route}")));
    }

    [Fact]
    public async Task Only_the_super_admin_may_read_the_list()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);
            await Assert.ThrowsAnyAsync<Exception>(() => scope.Send(new GetServerErrorsQuery()));
        });
    }

    [Fact]
    public async Task Rows_older_than_30_days_are_removed_when_pruning()
    {
        var old = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-31);
        var key = Guid.NewGuid().ToString("N").PadRight(64, '0');

        await InScope(async scope =>
        {
            scope.Db.ServerErrors.Add(new ServerError
            {
                Id = Guid.NewGuid(), Key = key, Day = old, ExceptionType = "X", Location = "Y", Route = "Z", Count = 1,
                FirstSeenAt = DateTime.UtcNow.AddDays(-31), LastSeenAt = DateTime.UtcNow.AddDays(-31), Version = "1",
            });
            await scope.Db.SaveChangesAsync();
            await scope.Send(new FlushServerErrorsCommand(Prune: true));
        });

        Assert.False(await InScope(scope => scope.Db.ServerErrors.AnyAsync(e => e.Key == key)));
    }
}
