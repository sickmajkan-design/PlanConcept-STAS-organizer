using System.Text;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Maintenance.Commands.CatchUpReminders;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Reminders that are due go out at the first sign-in, not a day later, and only
/// ever once: what someone deleted is not brought back.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CatchUpRemindersTests : IntegrationTestBase
{
    public CatchUpRemindersTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private async Task<int> NotificationsAboutAsync(Guid adminId, string fileName) =>
        await InScope(scope => scope.Db.Notifications
            .CountAsync(n => n.UserId == adminId
                && n.Type == NotificationType.DocumentExpiring
                && n.Body.Contains(fileName)));

    [Fact]
    public async Task A_document_about_to_lapse_reaches_the_admin_once_and_a_deleted_notification_stays_deleted()
    {
        var fileName = $"potvrda-{Guid.NewGuid():N}.pdf";
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(async scope =>
        {
            ActAs(scope, admin);
            var bytes = Encoding.UTF8.GetBytes("hello");

            await scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.Employee,
                OwnerId = employee.Id,
                Category = AttachmentCategory.Contract,
                FileName = fileName,
                SizeBytes = bytes.Length,
                Content = new MemoryStream(bytes),
                ExpiresAt = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
            });
        });

        Assert.Equal(0, await NotificationsAboutAsync(admin.Id, fileName));

        await InScope(scope => scope.Send(new CatchUpRemindersCommand { IgnoreGap = true }));

        Assert.Equal(1, await NotificationsAboutAsync(admin.Id, fileName));

        // Again: nothing new is due, so nobody is told twice.
        await InScope(scope => scope.Send(new CatchUpRemindersCommand { IgnoreGap = true }));

        Assert.Equal(1, await NotificationsAboutAsync(admin.Id, fileName));

        // The admin dismisses it. It was sent once and is not sent again.
        await InScope(async scope =>
        {
            var mine = await scope.Db.Notifications
                .Where(n => n.UserId == admin.Id && n.Body.Contains(fileName))
                .ToListAsync();
            scope.Db.Notifications.RemoveRange(mine);
            await scope.Db.SaveChangesAsync();
        });

        await InScope(scope => scope.Send(new CatchUpRemindersCommand { IgnoreGap = true }));

        Assert.Equal(0, await NotificationsAboutAsync(admin.Id, fileName));
    }

    [Fact]
    public async Task A_run_right_after_another_does_nothing()
    {
        await InScope(scope => scope.Send(new CatchUpRemindersCommand { IgnoreGap = true }));

        var second = await InScope(scope => scope.Send(new CatchUpRemindersCommand()));

        Assert.Equal(0, second);
    }
}
