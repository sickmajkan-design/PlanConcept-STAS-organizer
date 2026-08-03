using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.DeleteAttachment;
using Construction.Application.Features.Attachments.Commands.SendExpiryReminders;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Attachments.Models;
using Construction.Application.Features.Attachments.Queries.GetAttachmentContent;
using Construction.Application.Features.Attachments.Queries.GetAttachments;
using Construction.Application.Features.Attachments.Queries.GetExpiringDocuments;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Attachments run against PostgreSQL and the real filesystem storage: the
/// "exactly one owner" rule is a check constraint, the expiry sweep is a
/// conditional UPDATE that has to be atomic, and an upload that never touches
/// a disk is not an upload.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AttachmentTests : IntegrationTestBase
{
    public AttachmentTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static Stream Bytes(string content = "hello") =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    private static Task<AttachmentDto> UploadAsync(
        TestScope scope,
        AttachmentOwnerType ownerType,
        Guid ownerId,
        string fileName = "ugovor.pdf",
        AttachmentCategory category = AttachmentCategory.Contract,
        DateOnly? expiresAt = null,
        string content = "hello")
    {
        var bytes = Bytes(content);

        return scope.Send(new UploadAttachmentCommand
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            Category = category,
            FileName = fileName,
            SizeBytes = bytes.Length,
            Content = bytes,
            ExpiresAt = expiresAt
        });
    }

    // ---- storing and reading back ---------------------------------------

    [Fact]
    public async Task An_uploaded_file_comes_back_with_the_bytes_that_went_in()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope, AttachmentOwnerType.Employee, employee.Id, content: "ugovor 2026");
        });

        var read = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetAttachmentContentQuery(uploaded.Id));
        });

        using var reader = new StreamReader(read.Content);

        Assert.Equal("ugovor 2026", await reader.ReadToEndAsync());
        Assert.Equal("application/pdf", read.ContentType);
    }

    [Fact]
    public async Task The_content_type_comes_from_the_extension_not_the_upload()
    {
        // A client can claim any Content-Type it likes, and that value later
        // decides whether a browser renders the file.
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Project,
                project.Id,
                fileName: "slika.png",
                category: AttachmentCategory.Photo);
        });

        Assert.Equal("image/png", uploaded.ContentType);
    }

    [Fact]
    public async Task An_unacceptable_file_type_is_refused()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope =>
            {
                ActAs(scope, admin);
                return UploadAsync(
                    scope,
                    AttachmentOwnerType.Project,
                    project.Id,
                    fileName: "payload.exe",
                    category: AttachmentCategory.Other);
            }));
    }

    [Fact]
    public async Task Uploading_against_a_record_that_does_not_exist_is_refused()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, Guid.NewGuid());
        }));
    }

    [Fact]
    public async Task A_failed_upload_leaves_nothing_behind_in_storage()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var before = Directory.Exists(Fixture.StorageRoot)
            ? Directory.GetFiles(Fixture.StorageRoot, "*", SearchOption.AllDirectories).Length
            : 0;

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Tool, Guid.NewGuid());
        }));

        var after = Directory.Exists(Fixture.StorageRoot)
            ? Directory.GetFiles(Fixture.StorageRoot, "*", SearchOption.AllDirectories).Length
            : 0;

        Assert.Equal(before, after);
    }

    // ---- ownership -------------------------------------------------------

    [Fact]
    public async Task An_attachment_is_listed_only_against_its_own_record()
    {
        var first = await InScope(scope => TestData.SeedProjectAsync(scope));
        var second = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Project,
                first.Id,
                fileName: "nacrt.pdf",
                category: AttachmentCategory.SiteDocument);
        });

        var onSecond = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.Project,
                OwnerId = second.Id
            });
        });

        Assert.Empty(onSecond);
    }

    [Fact]
    public async Task Deleting_the_record_takes_its_files_with_it()
    {
        // What a data-erasure request needs, and what a discriminator column
        // could not give us.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id);
        });

        await InScope(async scope =>
        {
            // A hard delete, which is what an erasure request produces.
            await scope.Db.Database.ExecuteSqlRawAsync(
                "DELETE FROM employees WHERE \"Id\" = {0}", employee.Id);
        });

        var remaining = await InScope(scope => scope.Db.Attachments
            .IgnoreQueryFilters()
            .CountAsync(a => a.EmployeeId == employee.Id));

        Assert.Equal(0, remaining);
    }

    // ---- who may see what ------------------------------------------------

    [Fact]
    public async Task A_foreman_cannot_read_an_employees_documents()
    {
        // Contracts and medicals stop at Admin.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.Employee,
                OwnerId = employee.Id
            });
        }));
    }

    [Fact]
    public async Task An_employee_can_read_their_own_documents()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Employee,
                employee.Id,
                category: AttachmentCategory.MedicalCheck);
        });

        var mine = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.Employee,
                OwnerId = employee.Id
            });
        });

        Assert.Single(mine);
    }

    [Fact]
    public async Task An_employee_cannot_read_somebody_elses_documents()
    {
        var mine = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, mine.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.Employee,
                OwnerId = theirs.Id
            });
        }));
    }

    [Fact]
    public async Task Reading_someone_elses_document_by_id_reports_not_found()
    {
        // Not 403 — that would confirm the guessed id landed on a real file.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var otherEmployee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, otherEmployee.Id));

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id);
        });

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, worker, otherEmployee.Id);
            return scope.Send(new GetAttachmentContentQuery(uploaded.Id));
        }));
    }

    [Fact]
    public async Task A_worker_may_add_a_site_photo_but_not_a_contract()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var photo = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Project,
                project.Id,
                fileName: "gradiliste.jpg",
                category: AttachmentCategory.Photo);
        });

        Assert.Equal(AttachmentCategory.Photo, photo.Category);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Project,
                project.Id,
                fileName: "ugovor.pdf",
                category: AttachmentCategory.Contract);
        }));
    }

    [Fact]
    public async Task Only_an_administrator_can_delete()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var manager = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Project,
                project.Id,
                fileName: "nacrt.pdf",
                category: AttachmentCategory.SiteDocument);
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, manager);
            return scope.Send(new DeleteAttachmentCommand(uploaded.Id));
        }));
    }

    [Fact]
    public async Task Deleting_removes_the_bytes_as_well_as_the_row()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Project,
                project.Id,
                fileName: "nacrt.pdf",
                category: AttachmentCategory.SiteDocument);
        });

        var storageKey = await InScope(scope => scope.Db.Attachments
            .Where(a => a.Id == uploaded.Id)
            .Select(a => a.StorageKey)
            .SingleAsync());

        var path = Path.Combine(Fixture.StorageRoot, storageKey);
        Assert.True(File.Exists(path));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new DeleteAttachmentCommand(uploaded.Id));
        });

        // Hiding a medical record from a list does not satisfy a deletion
        // request; the file itself has to go.
        Assert.False(File.Exists(path));
    }

    // ---- expiry ----------------------------------------------------------

    [Fact]
    public async Task A_lapsed_document_shows_up_in_the_expiring_list()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var lapsed = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Employee,
                employee.Id,
                fileName: "lekarski.pdf",
                category: AttachmentCategory.MedicalCheck,
                expiresAt: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10));
        });

        var expiring = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetExpiringDocumentsQuery { WithinDays = 30 });
        });

        Assert.Contains(expiring, a => a.Id == lapsed.Id);
    }

    [Fact]
    public async Task A_document_expiring_beyond_the_window_is_left_out()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var distant = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Employee,
                employee.Id,
                fileName: "sertifikat.pdf",
                category: AttachmentCategory.Certificate,
                expiresAt: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(200));
        });

        var expiring = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetExpiringDocumentsQuery { WithinDays = 30 });
        });

        Assert.DoesNotContain(expiring, a => a.Id == distant.Id);
    }

    [Fact]
    public async Task A_reminder_goes_out_once_and_not_again()
    {
        // The guard against telling the office the same thing every morning
        // until somebody renews the certificate.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(
                scope,
                AttachmentOwnerType.Employee,
                employee.Id,
                fileName: "lekarski.pdf",
                category: AttachmentCategory.MedicalCheck,
                expiresAt: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5));
        });

        var first = await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));
        var second = await InScope(scope => scope.Send(new SendExpiryRemindersCommand()));

        Assert.True(first >= 1);
        Assert.Equal(0, second);
    }

    [Fact]
    public async Task A_photograph_is_never_given_an_expiry()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope =>
            {
                ActAs(scope, admin);
                return UploadAsync(
                    scope,
                    AttachmentOwnerType.Project,
                    project.Id,
                    fileName: "gradiliste.jpg",
                    category: AttachmentCategory.Photo,
                    expiresAt: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30));
            }));
    }
}
