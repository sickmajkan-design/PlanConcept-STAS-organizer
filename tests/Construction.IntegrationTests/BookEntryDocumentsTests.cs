using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.DeleteAttachment;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Attachments.Models;
using Construction.Application.Features.Attachments.Queries.GetAttachmentContent;
using Construction.Application.Features.Attachments.Queries.GetAttachmentCounts;
using Construction.Application.Features.Attachments.Queries.GetAttachments;
using Construction.Application.Features.Attachments.Queries.GetExpiringDocuments;
using Construction.Application.Features.Book;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Documents on the entries of the book of money: filed like any other document, but as private as the
/// entry itself — the role is not enough, the book grant is needed too.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BookEntryDocumentsTests : IntegrationTestBase
{
    public BookEntryDocumentsTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static int nextYear = 1850;

    private static int FreshYear() => Interlocked.Increment(ref nextYear);

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private async Task<User> SeedAsync(UserRole role, bool bookAccess)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, role);
            user.BookAccess = bookAccess;
            await scope.Db.SaveChangesAsync();

            return user;
        });
    }

    private async Task<(Guid EntryId, Guid AttachmentId, int Year)> EntryWithDocumentAsync(User writer)
    {
        var year = FreshYear();

        return await InScope(async scope =>
        {
            ActAs(scope, writer);

            var category = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts)
                .Select(c => c.Id)
                .SingleAsync();

            var entry = await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Cost,
                Amount = 120m,
                OccurredOn = new DateOnly(year, 3, 10),
                CategoryId = category,
                Description = "Racun za alat",
            });

            var bytes = TestFiles.WithSignature("racun.pdf", "racun");
            var attachment = await scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.BookEntry,
                OwnerId = entry.Id,
                Category = AttachmentCategory.Other,
                FileName = "racun.pdf",
                SizeBytes = bytes.Length,
                Content = new MemoryStream(bytes),
            });

            return (entry.Id, attachment.Id, year);
        });
    }

    [Fact]
    public async Task A_document_is_filed_on_an_entry_listed_counted_and_read_back_by_those_with_the_book()
    {
        var admin = await SeedAsync(UserRole.Admin, bookAccess: true);
        var (entryId, attachmentId, year) = await EntryWithDocumentAsync(admin);

        var (listed, counts, text, rows) = await InScope(async scope =>
        {
            ActAs(scope, admin);

            var list = await scope.Send(new GetAttachmentsQuery
            {
                OwnerType = AttachmentOwnerType.BookEntry,
                OwnerId = entryId,
            });
            var perEntry = await scope.Send(new GetAttachmentCountsQuery { OwnerType = AttachmentOwnerType.BookEntry });
            var content = await scope.Send(new GetAttachmentContentQuery(attachmentId));
            using var reader = new StreamReader(content.Content);
            var page = await scope.Send(new GetBookEntriesQuery
            {
                From = new DateOnly(year, 1, 1),
                To = new DateOnly(year, 12, 31),
            });

            return (list, perEntry, await reader.ReadToEndAsync(), page.Items);
        });

        Assert.Equal("racun.pdf", Assert.Single(listed).FileName);
        Assert.Equal(AttachmentOwnerType.BookEntry, listed[0].OwnerType);
        Assert.Equal(1, counts.Single(c => c.OwnerId == entryId).Count);
        Assert.EndsWith("racun", text);
        Assert.Equal(1, rows.Single(r => r.Id == entryId).DocumentCount);
    }

    [Fact]
    public async Task An_admin_without_the_book_grant_cannot_see_file_or_read_a_document_of_the_book()
    {
        var writer = await SeedAsync(UserRole.Admin, bookAccess: true);
        var outsider = await SeedAsync(UserRole.Admin, bookAccess: false);
        var (entryId, attachmentId, _) = await EntryWithDocumentAsync(writer);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, outsider);
            return scope.Send(new GetAttachmentsQuery { OwnerType = AttachmentOwnerType.BookEntry, OwnerId = entryId });
        }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, outsider);
            return scope.Send(new GetAttachmentCountsQuery { OwnerType = AttachmentOwnerType.BookEntry });
        }));

        // The file itself answers as if it were not there.
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, outsider);
            return scope.Send(new GetAttachmentContentQuery(attachmentId));
        }));

        var bytes = TestFiles.WithSignature("drugi.pdf");
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, outsider);
            return scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.BookEntry,
                OwnerId = entryId,
                Category = AttachmentCategory.Other,
                FileName = "drugi.pdf",
                SizeBytes = bytes.Length,
                Content = new MemoryStream(bytes),
            });
        }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, outsider);
            return scope.Send(new DeleteAttachmentCommand(attachmentId));
        }));
    }

    [Fact]
    public async Task The_all_documents_list_leaves_out_the_documents_of_the_book_for_those_without_the_grant()
    {
        var writer = await SeedAsync(UserRole.Admin, bookAccess: true);
        var outsider = await SeedAsync(UserRole.Admin, bookAccess: false);
        var (_, attachmentId, _) = await EntryWithDocumentAsync(writer);

        async Task<IReadOnlyList<AttachmentDto>> All(User user) => await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new GetExpiringDocumentsQuery { IncludeUndated = true });
        });

        Assert.Contains(await All(writer), d => d.Id == attachmentId);
        Assert.DoesNotContain(await All(outsider), d => d.Id == attachmentId);
    }

    [Fact]
    public async Task The_book_rows_count_the_documents_of_the_vehicle_cost_they_are_read_from()
    {
        var admin = await SeedAsync(UserRole.Admin, bookAccess: true);
        var year = FreshYear();

        var (expenseId, rows) = await InScope(async scope =>
        {
            ActAs(scope, admin);
            var vehicle = await TestData.SeedVehicleAsync(scope);
            var expense = new VehicleExpense
            {
                VehicleId = vehicle.Id,
                Kind = VehicleExpenseKind.Service,
                Amount = 80m,
                OccurredOn = new DateOnly(year, 5, 5),
                Status = VehicleExpenseStatus.Approved,
            };
            scope.Db.VehicleExpenses.Add(expense);
            await scope.Db.SaveChangesAsync();

            var bytes = TestFiles.WithSignature("servis.pdf");
            await scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.VehicleExpense,
                OwnerId = expense.Id,
                Category = AttachmentCategory.Other,
                FileName = "servis.pdf",
                SizeBytes = bytes.Length,
                Content = new MemoryStream(bytes),
            });

            var page = await scope.Send(new GetBookEntriesQuery
            {
                From = new DateOnly(year, 1, 1),
                To = new DateOnly(year, 12, 31),
            });

            return (expense.Id, page.Items);
        });

        Assert.Equal(1, rows.Single(r => r.Id == expenseId).DocumentCount);
    }
}
