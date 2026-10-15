using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.SetAttachmentSharing;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Attachments.Models;
using Construction.Application.Features.Attachments.Queries.ExportAttachments;
using Construction.Application.Features.Attachments.Queries.GetAttachmentContent;
using Construction.Application.Features.Attachments.Queries.GetAttachments;
using Construction.Application.Features.Attachments.Queries.GetMyDocuments;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Documents shared with the person they are about: a worker reaches only what the office shared,
/// on their own card or on the vehicle they currently drive, and nothing else.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AttachmentSharingTests : IntegrationTestBase
{
    public AttachmentSharingTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    private static Task<AttachmentDto> UploadAsync(
        TestScope scope,
        AttachmentOwnerType ownerType,
        Guid ownerId,
        string fileName = "dokument.pdf",
        AttachmentCategory category = AttachmentCategory.Certificate)
    {
        var bytes = new MemoryStream(TestFiles.WithSignature(fileName, "sadrzaj"));

        return scope.Send(new UploadAttachmentCommand
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            Category = category,
            FileName = fileName,
            SizeBytes = bytes.Length,
            Content = bytes
        });
    }

    private async Task ShareAsync(User admin, bool shared, params Guid[] ids) =>
        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new SetAttachmentSharingCommand { Ids = ids.ToList(), Shared = shared });
        });

    [Fact]
    public async Task Nothing_is_shared_until_the_office_shares_it()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id);
        });

        Assert.False(uploaded.SharedWithEmployee);

        var listed = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.Employee,
                OwnerId = employee.Id
            });
        });

        Assert.Empty(listed);

        // Closed by id as well, and indistinguishable from a file that does not exist.
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetAttachmentContentQuery(uploaded.Id));
        }));

        var mine = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetMyDocumentsQuery());
        });

        Assert.Empty(mine.Documents);
    }

    [Fact]
    public async Task A_shared_document_is_listed_opened_and_exported_but_only_by_its_person()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var colleague = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));
        var colleagueUser = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, colleague.Id));

        var shared = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id, "potvrda.pdf");
        });
        var hidden = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id, "interno.pdf", AttachmentCategory.Other);
        });

        await ShareAsync(admin, true, shared.Id);

        var mine = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetMyDocumentsQuery());
        });

        Assert.Equal(shared.Id, Assert.Single(mine.Documents).Id);

        await InScope(async scope =>
        {
            ActAs(scope, worker, employee.Id);
            var content = await scope.Send(new GetAttachmentContentQuery(shared.Id));
            await content.Content.DisposeAsync();
        });

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetAttachmentContentQuery(hidden.Id));
        }));

        // A colleague gets neither the shared one nor the list of someone else's card.
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, colleagueUser, colleague.Id);
            return scope.Send(new GetAttachmentContentQuery(shared.Id));
        }));

        var colleagueMine = await InScope(scope =>
        {
            ActAs(scope, colleagueUser, colleague.Id);
            return scope.Send(new GetMyDocumentsQuery());
        });

        Assert.Empty(colleagueMine.Documents);

        // Taking the sharing back closes it again.
        await ShareAsync(admin, false, shared.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetAttachmentContentQuery(shared.Id));
        }));
    }

    [Fact]
    public async Task The_export_leaves_out_what_was_not_shared()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var hidden = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id, "interno.pdf");
        });

        var export = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new ExportAttachmentsQuery([hidden.Id]));
        });

        using var archive = new System.IO.Compression.ZipArchive(export.Content);
        Assert.DoesNotContain(archive.Entries, e => e.Name == "interno.pdf");
    }

    [Fact]
    public async Task A_driver_sees_the_dates_and_shared_documents_of_the_vehicle_they_drive_and_only_that()
    {
        var driverEmployee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var other = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var driver = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, driverEmployee.Id));
        var otherUser = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, other.Id));

        var vehicle = await InScope(async scope =>
        {
            var v = await TestData.SeedVehicleAsync(scope);
            v.AssignedEmployeeId = driverEmployee.Id;
            v.InsuranceValidUntil = new DateOnly(2027, 1, 1);
            scope.Db.VehicleTolls.Add(new VehicleToll
            {
                VehicleId = v.Id,
                Type = VehicleTollType.Vignette,
                Country = "SI",
                Status = VehicleTollStatus.Paid,
                ValidUntil = new DateOnly(2026, 12, 31)
            });
            await scope.Db.SaveChangesAsync();
            return v;
        });

        var policy = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Vehicle, vehicle.Id, "polica.pdf", AttachmentCategory.Insurance);
        });
        var internalDoc = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Vehicle, vehicle.Id, "ugovor-najam.pdf", AttachmentCategory.Contract);
        });

        await ShareAsync(admin, true, policy.Id);

        var mine = await InScope(scope =>
        {
            ActAs(scope, driver, driverEmployee.Id);
            return scope.Send(new GetMyDocumentsQuery());
        });

        var seen = Assert.Single(mine.Vehicles);
        Assert.Equal(new DateOnly(2027, 1, 1), seen.InsuranceValidUntil);
        Assert.Equal("SI", Assert.Single(seen.Tolls).Country);
        Assert.Equal(policy.Id, Assert.Single(seen.Documents).Id);

        // The same through the list and the download.
        var listed = await InScope(scope =>
        {
            ActAs(scope, driver, driverEmployee.Id);
            return scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.Vehicle,
                OwnerId = vehicle.Id
            });
        });

        Assert.Equal(policy.Id, Assert.Single(listed).Id);

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, driver, driverEmployee.Id);
            return scope.Send(new GetAttachmentContentQuery(internalDoc.Id));
        }));

        // Somebody else's access, and the driver's own once the van is handed back.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, otherUser, other.Id);
            return scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.Vehicle,
                OwnerId = vehicle.Id
            });
        }));

        await InScope(async scope =>
        {
            var v = await scope.Db.Vehicles.SingleAsync(x => x.Id == vehicle.Id);
            v.AssignedEmployeeId = null;
            await scope.Db.SaveChangesAsync();
        });

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, driver, driverEmployee.Id);
            return scope.Send(new GetAttachmentContentQuery(policy.Id));
        }));

        var afterReturn = await InScope(scope =>
        {
            ActAs(scope, driver, driverEmployee.Id);
            return scope.Send(new GetMyDocumentsQuery());
        });

        Assert.Empty(afterReturn.Vehicles);
    }

    [Fact]
    public async Task Only_documents_on_a_person_or_a_vehicle_can_be_shared_and_a_failure_changes_nothing()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var onEmployee = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id);
        });
        var onProject = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Project, project.Id);
        });

        await Assert.ThrowsAsync<ConflictException>(() =>
            ShareAsync(admin, true, onEmployee.Id, onProject.Id));

        var after = await InScope(scope => scope.Db.Attachments.AsNoTracking()
            .Where(a => a.Id == onEmployee.Id)
            .Select(a => a.SharedWithEmployee)
            .SingleAsync());

        Assert.False(after);

        await Assert.ThrowsAsync<NotFoundException>(() => ShareAsync(admin, true, Guid.NewGuid()));
    }

    [Theory]
    [InlineData(UserRole.Worker)]
    [InlineData(UserRole.Foreman)]
    public async Task Sharing_is_the_offices_decision(UserRole role)
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var user = await InScope(scope => TestData.SeedUserAsync(scope, role, employee.Id));

        var uploaded = await InScope(scope =>
        {
            ActAs(scope, admin);
            return UploadAsync(scope, AttachmentOwnerType.Employee, employee.Id);
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new SetAttachmentSharingCommand { Ids = [uploaded.Id], Shared = true });
        }));
    }
}
