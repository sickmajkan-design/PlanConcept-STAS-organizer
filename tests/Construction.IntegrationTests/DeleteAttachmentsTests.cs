using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.DeleteAttachments;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Picking documents nobody needs and removing them for good: the file and the record, nothing left behind, with a
/// retention date still protecting what it protects.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class DeleteAttachmentsTests : IntegrationTestBase
{
    public DeleteAttachmentsTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private Task<Guid> UploadAsync(User admin, Guid employeeId, string name, DateOnly? retainUntil = null) =>
        InScope(async scope =>
        {
            ActAs(scope, admin);

            var dto = await scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.Employee,
                OwnerId = employeeId,
                Category = AttachmentCategory.Other,
                FileName = name,
                SizeBytes = 100,
                Content = new MemoryStream(TestFiles.WithSignature(name, name)),
                RetainUntil = retainUntil,
            });

            return dto.Id;
        });

    private Task<DeleteAttachmentsResultDto> DeleteAsync(User who, params Guid[] ids) =>
        InScope(scope =>
        {
            ActAs(scope, who);
            return scope.Send(new DeleteAttachmentsCommand { Ids = ids });
        });

    private Task<string> KeyOfAsync(Guid id) =>
        InScope(scope => scope.Db.Attachments.IgnoreQueryFilters().Where(a => a.Id == id).Select(a => a.StorageKey).SingleAsync());

    private Task<bool> FileExistsAsync(string key) =>
        InScope(async scope =>
        {
            await using var stream = await scope.Storage.OpenReadAsync(key);
            return stream is not null;
        });

    private Task<bool> RowExistsAsync(Guid id) =>
        InScope(scope => scope.Db.Attachments.IgnoreQueryFilters().AnyAsync(a => a.Id == id));

    [Fact]
    public async Task The_picked_documents_lose_their_file_and_their_record_and_the_rest_stay()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var first = await UploadAsync(admin, employee.Id, "prvi.pdf");
        var second = await UploadAsync(admin, employee.Id, "drugi.pdf");
        var kept = await UploadAsync(admin, employee.Id, "ostaje.pdf");
        var firstKey = await KeyOfAsync(first);
        var secondKey = await KeyOfAsync(second);
        var keptKey = await KeyOfAsync(kept);

        var result = await DeleteAsync(admin, first, second);

        Assert.Equal(2, result.Removed);
        Assert.Equal(200, result.FreedBytes);
        Assert.False(await RowExistsAsync(first), "no trace is left");
        Assert.False(await RowExistsAsync(second));
        Assert.False(await FileExistsAsync(firstKey));
        Assert.False(await FileExistsAsync(secondKey));
        Assert.True(await RowExistsAsync(kept));
        Assert.True(await FileExistsAsync(keptKey));
    }

    [Fact]
    public async Task A_document_under_a_retention_date_is_kept_and_counted()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var plain = await UploadAsync(admin, employee.Id, "obican.pdf");
        var retained = await UploadAsync(admin, employee.Id, "ugovor.pdf", DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2));
        var retainedKey = await KeyOfAsync(retained);

        var result = await DeleteAsync(admin, plain, retained);

        Assert.Equal(1, result.Removed);
        Assert.Equal(1, result.KeptForRetention);
        Assert.True(await RowExistsAsync(retained));
        Assert.True(await FileExistsAsync(retainedKey));
    }

    [Fact]
    public async Task An_id_that_is_already_gone_is_counted_not_an_error()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var real = await UploadAsync(admin, employee.Id, "jedan.pdf");

        var result = await DeleteAsync(admin, real, Guid.NewGuid());

        Assert.Equal(1, result.Removed);
        Assert.Equal(1, result.NotFound);
    }

    [Theory]
    [InlineData(UserRole.Worker)]
    [InlineData(UserRole.Foreman)]
    [InlineData(UserRole.ProjectManager)]
    public async Task Only_admin_and_above_may_delete_in_bulk(UserRole role)
    {
        var someone = await InScope(scope => TestData.SeedUserAsync(scope, role));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => DeleteAsync(someone, Guid.NewGuid()));
    }

    [Fact]
    public async Task Nothing_picked_or_too_many_is_refused()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ValidationException>(() => DeleteAsync(admin));
        await Assert.ThrowsAsync<ValidationException>(() =>
            DeleteAsync(admin, Enumerable.Range(0, DeleteAttachmentsCommand.MaxIds + 1).Select(_ => Guid.NewGuid()).ToArray()));
    }
}
