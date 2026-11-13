using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// How much one record may hold and how much one account may send in a day, and that a Foreman cannot add to a
/// person's file (which they cannot read either).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AttachmentQuotaTests : IntegrationTestBase
{
    public AttachmentQuotaTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static async Task<object> Upload(TestScope scope, Guid employeeId) =>
        await scope.Send(new UploadAttachmentCommand
        {
            OwnerType = AttachmentOwnerType.Employee,
            OwnerId = employeeId,
            Category = AttachmentCategory.Other,
            FileName = "a.pdf",
            SizeBytes = 100,
            Content = new MemoryStream(TestFiles.WithSignature("a.pdf")),
        });

    /// <summary>Rows that stand for files already stored, without storing any bytes.</summary>
    private Task SeedStoredAsync(Guid employeeId, int count, long sizeEach, Guid? uploadedBy)
    {
        return InScope(async scope =>
        {
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();

                scope.Db.Attachments.Add(new Attachment
                {
                    Id = id,
                    FileName = $"seed{i}.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = sizeEach,
                    Category = AttachmentCategory.Other,
                    EmployeeId = employeeId,
                    StorageKey = $"employees/{employeeId:N}/{id:N}.pdf",
                    UploadedByUserId = uploadedBy,
                });
            }

            await scope.Db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task A_record_stops_taking_files_at_the_limit_and_the_next_record_is_unaffected()
    {
        var full = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var other = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await SeedStoredAsync(full.Id, AttachmentQuota.MaxFilesPerRecord, 1, uploadedBy: null);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return Upload(scope, full.Id);
        }));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return Upload(scope, other.Id);
        });
    }

    [Fact]
    public async Task One_account_cannot_send_more_than_the_daily_amount_but_another_can()
    {
        var target = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var elsewhere = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var heavy = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var other = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        // One file that brings this account to a hair under the daily bytes.
        await SeedStoredAsync(elsewhere.Id, 1, AttachmentQuota.MaxBytesPerAccountPerDay - 50, uploadedBy: heavy.Id);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, heavy);
            return Upload(scope, target.Id);
        }));

        await InScope(scope =>
        {
            ActAs(scope, other);
            return Upload(scope, target.Id);
        });
    }

    [Fact]
    public async Task A_foreman_cannot_add_a_file_to_an_employee()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return Upload(scope, employee.Id);
        }));
    }

    [Fact]
    public async Task The_office_can_still_add_a_file_to_an_employee()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return Upload(scope, employee.Id);
        });
    }
}
