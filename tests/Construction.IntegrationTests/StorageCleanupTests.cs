using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.DeleteAttachment;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.StorageCleanup;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// What the Super Admin can clean off the server by hand: files nothing points at, documents of records that have been
/// deleted, and the traces of deleted documents. Each removes the bytes and the row for good, and keeps what a
/// retention date still protects.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class StorageCleanupTests : IntegrationTestBase
{
    private readonly DatabaseFixture _fixture;

    public StorageCleanupTests(DatabaseFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private Task<StorageCleanupResultDto> RunAsync(User owner, StorageCleanupKind kind) =>
        InScope(scope =>
        {
            ActAs(scope, owner);
            return scope.Send(new RunStorageCleanupCommand(kind));
        });

    private Task<StorageCleanupDto> ScanAsync(User owner) =>
        InScope(scope =>
        {
            ActAs(scope, owner);
            return scope.Send(new GetStorageCleanupQuery());
        });

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

    private Task<string> KeyOfAsync(Guid attachmentId) =>
        InScope(scope => scope.Db.Attachments.IgnoreQueryFilters()
            .Where(a => a.Id == attachmentId).Select(a => a.StorageKey).SingleAsync());

    private Task<bool> ExistsAsync(string key) =>
        InScope(async scope =>
        {
            await using var stream = await scope.Storage.OpenReadAsync(key);
            return stream is not null;
        });

    private Task<bool> RowExistsAsync(Guid attachmentId) =>
        InScope(scope => scope.Db.Attachments.IgnoreQueryFilters().AnyAsync(a => a.Id == attachmentId));

    /// <summary>Makes a stored file look older than the grace period, which an upload in flight is protected by.</summary>
    private void Age(string key) =>
        File.SetLastWriteTimeUtc(Path.Combine(_fixture.StorageRoot, key), DateTime.UtcNow.AddDays(-3));

    [Fact]
    public async Task A_file_nothing_points_at_is_removed_and_a_young_one_or_a_referenced_one_is_not()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var orphan = $"orphans/{Guid.NewGuid():N}/old.pdf";
        var young = $"orphans/{Guid.NewGuid():N}/young.pdf";
        var referencedId = await UploadAsync(admin, employee.Id, "ugovor.pdf");
        var referenced = await KeyOfAsync(referencedId);

        await InScope(async scope =>
        {
            await scope.Storage.SaveAsync(orphan, new MemoryStream("x"u8.ToArray()), "application/pdf");
            await scope.Storage.SaveAsync(young, new MemoryStream("y"u8.ToArray()), "application/pdf");
        });

        Age(orphan);
        Age(referenced);

        var scan = await ScanAsync(owner);
        Assert.Contains(scan.Categories, c => c.Kind == StorageCleanupKind.OrphanFiles && c.Count >= 1);

        await RunAsync(owner, StorageCleanupKind.OrphanFiles);

        Assert.False(await ExistsAsync(orphan), "the orphan is gone");
        Assert.True(await ExistsAsync(young), "a file younger than the grace period may be an upload in flight");
        Assert.True(await ExistsAsync(referenced), "a file a document points at is never touched");
        Assert.True(await RowExistsAsync(referencedId));
    }

    [Fact]
    public async Task Documents_of_a_deleted_employee_go_for_good_except_those_a_retention_date_protects()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var plain = await UploadAsync(admin, employee.Id, "potvrda.pdf");
        var retained = await UploadAsync(
            admin, employee.Id, "ugovor.pdf", DateOnly.FromDateTime(DateTime.UtcNow).AddYears(3));
        var plainKey = await KeyOfAsync(plain);
        var retainedKey = await KeyOfAsync(retained);

        // Documents of a person who still exists are not touched.
        var stillHere = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var untouched = await UploadAsync(admin, stillHere.Id, "ostaje.pdf");
        var untouchedKey = await KeyOfAsync(untouched);

        await InScope(async scope =>
        {
            var row = await scope.Db.Employees.SingleAsync(e => e.Id == employee.Id);
            scope.Db.Employees.Remove(row);
            await scope.Db.SaveChangesAsync();
        });

        var scan = await ScanAsync(owner);
        var category = scan.Categories.Single(c => c.Kind == StorageCleanupKind.DeletedRecordDocuments);
        Assert.True(category.Count >= 1);
        Assert.True(category.KeptForRetention >= 1);

        var result = await RunAsync(owner, StorageCleanupKind.DeletedRecordDocuments);

        Assert.True(result.RemovedRows >= 1);
        Assert.False(await RowExistsAsync(plain), "the row is gone, not just hidden");
        Assert.False(await ExistsAsync(plainKey), "and so are the bytes");
        Assert.True(await RowExistsAsync(retained));
        Assert.True(await ExistsAsync(retainedKey), "a retention date still protects it");
        Assert.True(await RowExistsAsync(untouched));
        Assert.True(await ExistsAsync(untouchedKey));
    }

    [Fact]
    public async Task The_traces_of_deleted_documents_are_removed_and_live_documents_are_not()
    {
        var owner = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var deleted = await UploadAsync(admin, employee.Id, "stari.pdf");
        var live = await UploadAsync(admin, employee.Id, "novi.pdf");
        var deletedKey = await KeyOfAsync(deleted);

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new DeleteAttachmentCommand(deleted));
        });

        // The ordinary delete takes the bytes and keeps the row as a trace.
        Assert.False(await ExistsAsync(deletedKey));
        Assert.True(await RowExistsAsync(deleted));

        await RunAsync(owner, StorageCleanupKind.DeletionTraces);

        Assert.False(await RowExistsAsync(deleted), "the trace is gone");
        Assert.True(await RowExistsAsync(live));
    }

    [Fact]
    public async Task Only_the_super_admin_may_look_or_clean()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => ScanAsync(admin));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => RunAsync(admin, StorageCleanupKind.OrphanFiles));
    }
}
