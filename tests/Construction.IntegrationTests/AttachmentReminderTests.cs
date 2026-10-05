using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.SendExpiryReminders;
using Construction.Application.Features.Attachments.Commands.UpdateAttachment;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Attachments.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A document can carry its own reminder lead times (a visa at 90 and 30 days) instead of the
/// admin's general one. Every admin is told; the person who added it is named.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AttachmentReminderTests : IntegrationTestBase
{
    public AttachmentReminderTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private Task<AttachmentDto> UploadVisaAsync(User uploader, Guid employeeId, DateOnly? expires, params int[] reminders) =>
        InScope(scope =>
        {
            ActAs(scope, uploader);
            var bytes = new MemoryStream(Encoding.UTF8.GetBytes("viza"));

            return scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.Employee,
                OwnerId = employeeId,
                Category = AttachmentCategory.Certificate,
                FileName = "viza.pdf",
                SizeBytes = bytes.Length,
                Content = bytes,
                ExpiresAt = expires,
                ReminderDays = reminders.Length == 0 ? null : reminders.ToList()
            });
        });

    private Task<List<Notification>> NotificationsFor(User user, Guid attachmentId) =>
        InScope(async scope => (await scope.Db.Notifications
                .Where(n => n.UserId == user.Id && n.Type == NotificationType.DocumentExpiring)
                .ToListAsync())
            .Where(n => n.DataJson != null && n.DataJson.Contains(attachmentId.ToString()))
            .ToList());

    [Fact]
    public async Task The_own_lead_time_replaces_the_general_one()
    {
        // 80 days left: the general rule (30) would be silent, the document's own 90 is not.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var visa = await UploadVisaAsync(admin, employee.Id, Today.AddDays(80), 90);

        Assert.Equal(new[] { 90 }, visa.ReminderDays);

        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        Assert.Single(await NotificationsFor(admin, visa.Id));
    }

    [Fact]
    public async Task Another_admin_is_told_too_and_the_uploader_is_named()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var uploader = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var other = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var visa = await UploadVisaAsync(uploader, employee.Id, Today.AddDays(20), 30);
        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        var mine = Assert.Single(await NotificationsFor(uploader, visa.Id));
        var theirs = Assert.Single(await NotificationsFor(other, visa.Id));

        Assert.Contains("addedByYou", mine.DataJson);
        Assert.DoesNotContain("addedByYou", theirs.DataJson);
    }

    [Fact]
    public async Task Each_lead_time_is_reported_once_and_a_sweep_that_repeats_stays_quiet()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        // Both 90 and 60 are already reached: one message, not two, and nothing the next time.
        var visa = await UploadVisaAsync(admin, employee.Id, Today.AddDays(50), 90, 60);

        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));
        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        Assert.Single(await NotificationsFor(admin, visa.Id));
    }

    [Fact]
    public async Task A_later_lead_time_still_reports_after_an_earlier_one()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var visa = await UploadVisaAsync(admin, employee.Id, Today.AddDays(80), 90, 30);
        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));
        Assert.Single(await NotificationsFor(admin, visa.Id));

        // Time passes: the document is now 20 days from lapsing, so 30 has been reached as well.
        await InScope(async scope =>
        {
            var row = await scope.Db.Attachments.SingleAsync(a => a.Id == visa.Id);
            row.ExpiresAt = Today.AddDays(20);
            await scope.Db.SaveChangesAsync();
        });
        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        Assert.Equal(2, (await NotificationsFor(admin, visa.Id)).Count);
    }

    [Fact]
    public async Task Renewing_the_document_starts_the_reminders_over()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var visa = await UploadVisaAsync(admin, employee.Id, Today.AddDays(10), 30);
        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new UpdateAttachmentCommand
            {
                Id = visa.Id,
                Category = AttachmentCategory.Certificate,
                ExpiresAt = Today.AddDays(20),
                ReminderDays = [30]
            });
        });
        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        Assert.Equal(2, (await NotificationsFor(admin, visa.Id)).Count);
    }

    [Fact]
    public async Task A_document_without_own_lead_times_keeps_the_general_rule()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var visa = await UploadVisaAsync(admin, employee.Id, Today.AddDays(5));

        Assert.Empty(visa.ReminderDays);

        await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        Assert.Single(await NotificationsFor(admin, visa.Id));
    }

    [Fact]
    public async Task A_reminder_needs_an_expiry_date_and_a_sensible_number_of_days()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ValidationException>(() => UploadVisaAsync(admin, employee.Id, null, 30));
        await Assert.ThrowsAsync<ValidationException>(() => UploadVisaAsync(admin, employee.Id, Today.AddDays(90), 0));
        await Assert.ThrowsAsync<ValidationException>(() => UploadVisaAsync(admin, employee.Id, Today.AddDays(90), 400));
        await Assert.ThrowsAsync<ValidationException>(() => UploadVisaAsync(admin, employee.Id, Today.AddDays(90), 1, 2, 3, 4, 5, 6));
    }
}
