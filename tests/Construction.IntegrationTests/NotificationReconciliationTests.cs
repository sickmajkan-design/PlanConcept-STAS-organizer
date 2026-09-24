using System.Text.Json;
using Construction.Application.Features.Employees.Commands.DeleteEmployee;
using Construction.Application.Features.Maintenance.Commands.PurgeOrphanedNotifications;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A notification points at records by id inside a JSON blob, so nothing in the
/// database removes it when the record goes. The sweep does.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class NotificationReconciliationTests : IntegrationTestBase
{
    public NotificationReconciliationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static async Task<Guid> SeedNotificationAsync(
        TestScope scope, Guid userId, NotificationType type, object data)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = "t",
            Body = "b",
            DataJson = JsonSerializer.Serialize(data)
        };

        scope.Db.Notifications.Add(notification);
        await scope.Db.SaveChangesAsync();

        return notification.Id;
    }

    [Fact]
    public async Task A_notification_goes_when_the_employee_it_names_is_deleted_even_if_its_type_is_about_a_project()
    {
        using var scope = Fixture.CreateScope();

        var recipient = await TestData.SeedUserAsync(scope);
        var project = await TestData.SeedProjectAsync(scope);
        var gone = await TestData.SeedEmployeeAsync(scope);
        var kept = await TestData.SeedEmployeeAsync(scope);

        // "New project assigned" carries the project id first and the employee id
        // second. The sweep once checked only the first, so deleting the employee
        // left this behind.
        var orphan = await SeedNotificationAsync(scope, recipient.Id, NotificationType.ProjectAssigned,
            new { projectId = project.Id, employeeId = gone.Id });
        var stays = await SeedNotificationAsync(scope, recipient.Id, NotificationType.ProjectAssigned,
            new { projectId = project.Id, employeeId = kept.Id });

        scope.CurrentUser.SignInAs(recipient.Id, UserRole.SuperAdmin, null, recipient.Email);
        await scope.Send(new DeleteEmployeeCommand(gone.Id));

        var removed = await scope.Send(new PurgeOrphanedNotificationsCommand());

        Assert.True(removed >= 1);
        Assert.False(await scope.Db.Notifications.AnyAsync(n => n.Id == orphan));
        Assert.True(await scope.Db.Notifications.AnyAsync(n => n.Id == stays));
    }

    [Fact]
    public async Task Free_text_notifications_are_never_touched()
    {
        using var scope = Fixture.CreateScope();

        var recipient = await TestData.SeedUserAsync(scope);
        var announcement = await SeedNotificationAsync(scope, recipient.Id, NotificationType.GeneralAnnouncement, new { });

        await scope.Send(new PurgeOrphanedNotificationsCommand());

        Assert.True(await scope.Db.Notifications.AnyAsync(n => n.Id == announcement));
    }
}
