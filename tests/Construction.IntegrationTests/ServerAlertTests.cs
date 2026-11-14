using Construction.Application.Features.SystemStatus;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// When the application comes back after being down, the Super Admins are told once, by the channels they chose.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ServerAlertTests : IntegrationTestBase
{
    private const string OutageTitle = "Server nije bio dostupan";

    public ServerAlertTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task An_outage_is_announced_once_by_push_and_by_e_mail()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        await InScope(async scope =>
        {
            scope.Db.ServiceOutages.Add(new ServiceOutage { StartedAt = DateTime.UtcNow.AddMinutes(-20), EndedAt = DateTime.UtcNow.AddMinutes(-2) });
            await scope.Db.SaveChangesAsync();
        });

        await InScope(scope => scope.Send(new SendServerAlertsCommand()));
        await InScope(scope => scope.Send(new SendServerAlertsCommand()));

        Assert.Equal(1, await InScope(scope => scope.Db.Notifications.CountAsync(n => n.UserId == owner.Id && n.Title == OutageTitle)));
        // The payload is jsonb, so it is read back and searched here rather than in SQL.
        var emails = await InScope(scope => scope.Db.OutboxMessages.Where(m => m.Type == OutboxMessageType.Email).ToListAsync());
        Assert.Equal(1, emails.Count(m => m.PayloadJson.Contains(owner.Email) && m.PayloadJson.Contains(OutageTitle)));
    }

    [Fact]
    public async Task A_kind_switched_off_sends_nothing()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(owner.Id, owner.Role, null, owner.Email);
            await scope.Send(new UpdateServerAlertSettingCommand(ServerAlertKeys.Outage, false, false));
        });

        var before = await InScope(scope => scope.Db.Notifications.CountAsync(n => n.UserId == owner.Id));

        await InScope(async scope =>
        {
            scope.Db.ServiceOutages.Add(new ServiceOutage { StartedAt = DateTime.UtcNow.AddMinutes(-20), EndedAt = DateTime.UtcNow.AddMinutes(-1) });
            await scope.Db.SaveChangesAsync();
        });
        await InScope(scope => scope.Send(new SendServerAlertsCommand()));

        Assert.Equal(before, await InScope(scope => scope.Db.Notifications.CountAsync(n => n.UserId == owner.Id)));

        // Put the shared default back for the other tests.
        await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(owner.Id, owner.Role, null, owner.Email);
            await scope.Send(new UpdateServerAlertSettingCommand(ServerAlertKeys.Outage, true, true));
        });
    }
}
