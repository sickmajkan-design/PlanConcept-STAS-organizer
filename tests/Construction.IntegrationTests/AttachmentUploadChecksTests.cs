using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.UpdateAttachment;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Attachments.Models;
using Construction.Application.Features.Attachments.Queries.GetAttachmentTags;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// What an upload must satisfy beyond its extension: the content has to be what the name says, the size
/// limit is 25 MB, and a document may carry a title and labels that are kept tidy.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AttachmentUploadChecksTests : IntegrationTestBase
{
    public AttachmentUploadChecksTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private async Task<T> AsAdminAsync<T>(Func<TestScope, Guid, Task<T>> action)
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        return await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);
            return action(scope, employee.Id);
        });
    }

    private static Task<AttachmentDto> Upload(
        TestScope scope, Guid ownerId, string fileName, byte[] bytes,
        string? title = null, List<string>? tags = null, long? declaredSize = null) =>
        scope.Send(new UploadAttachmentCommand
        {
            OwnerType = AttachmentOwnerType.Employee,
            OwnerId = ownerId,
            Category = AttachmentCategory.Other,
            FileName = fileName,
            SizeBytes = declaredSize ?? bytes.Length,
            Content = new MemoryStream(bytes),
            Title = title,
            Tags = tags,
        });

    [Fact]
    public async Task A_file_the_virus_scan_recognises_is_refused_whoever_sends_it()
    {
        var infected = TestFiles.WithSignature("ugovor.pdf", FakeVirusScanner.Eicar);

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsAdminAsync((scope, owner) => Upload(scope, owner, "ugovor.pdf", infected)));
    }

    [Fact]
    public async Task A_clean_file_goes_through_the_scan_and_is_stored()
    {
        var clean = TestFiles.WithSignature("ugovor.pdf", "obican ugovor");

        var stored = await AsAdminAsync(async (scope, owner) =>
        {
            var before = scope.VirusScanner.Scans;
            var result = await Upload(scope, owner, "ugovor.pdf", clean);

            Assert.True(scope.VirusScanner.Scans > before, "the upload was scanned");

            return result;
        });

        Assert.Equal("ugovor.pdf", stored.FileName);
    }

    [Fact]
    public async Task A_program_renamed_to_pdf_is_refused()
    {
        var exe = Encoding.ASCII.GetBytes("MZ\u0090\u0000\u0003\u0000\u0000\u0000 this program cannot be run in DOS mode");

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsAdminAsync((scope, owner) => Upload(scope, owner, "ugovor.pdf", exe)));
    }

    [Fact]
    public async Task A_text_file_with_binary_content_is_refused_and_plain_text_is_not()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            AsAdminAsync((scope, owner) => Upload(scope, owner, "bilješke.txt", [0x41, 0x00, 0x42, 0x00, 0x00, 0x01])));

        var ok = await AsAdminAsync((scope, owner) =>
            Upload(scope, owner, "bilješke.txt", Encoding.UTF8.GetBytes("Običan tekst, čćžšđ")));

        Assert.Equal("text/plain", ok.ContentType);
    }

    [Theory]
    [InlineData("racun.pdf", "application/pdf")]
    [InlineData("slika.jpg", "image/jpeg")]
    [InlineData("slika.png", "image/png")]
    [InlineData("tabela.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("stari.doc", "application/msword")]
    public async Task A_file_whose_content_fits_its_name_is_accepted(string fileName, string contentType)
    {
        var uploaded = await AsAdminAsync((scope, owner) =>
            Upload(scope, owner, fileName, TestFiles.WithSignature(fileName, "sadrzaj")));

        Assert.Equal(contentType, uploaded.ContentType);
    }

    [Fact]
    public async Task A_csv_file_is_accepted()
    {
        var uploaded = await AsAdminAsync((scope, owner) =>
            Upload(scope, owner, "uvoz.csv", Encoding.UTF8.GetBytes("a;b\n1;2")));

        Assert.Equal("text/csv", uploaded.ContentType);
    }

    [Fact]
    public async Task The_limit_is_25_megabytes_to_the_byte()
    {
        Assert.Equal(25L * 1024 * 1024, AttachmentRules.MaxSizeBytes);

        var bytes = TestFiles.WithSignature("a.pdf");

        await AsAdminAsync((scope, owner) =>
            Upload(scope, owner, "a.pdf", bytes, declaredSize: AttachmentRules.MaxSizeBytes));

        await Assert.ThrowsAsync<ValidationException>(() =>
            AsAdminAsync((scope, owner) =>
                Upload(scope, owner, "a.pdf", bytes, declaredSize: AttachmentRules.MaxSizeBytes + 1)));
    }

    [Fact]
    public async Task A_title_and_labels_are_kept_and_the_labels_are_tidied()
    {
        var uploaded = await AsAdminAsync((scope, owner) => Upload(
            scope, owner, "viza.pdf", TestFiles.WithSignature("viza.pdf"),
            title: "  Viza Marko  ",
            tags: ["  viza ", "VIZA", "2026", "", "  "]));

        Assert.Equal("Viza Marko", uploaded.Title);
        Assert.Equal(["viza", "2026"], uploaded.Tags);
    }

    [Fact]
    public async Task Too_many_or_too_long_labels_and_too_long_a_title_are_refused()
    {
        var bytes = TestFiles.WithSignature("a.pdf");
        var eleven = Enumerable.Range(1, AttachmentRules.MaxTags + 1).Select(i => $"oznaka{i}").ToList();

        await Assert.ThrowsAsync<ValidationException>(() =>
            AsAdminAsync((scope, owner) => Upload(scope, owner, "a.pdf", bytes, tags: eleven)));

        await Assert.ThrowsAsync<ValidationException>(() =>
            AsAdminAsync((scope, owner) =>
                Upload(scope, owner, "a.pdf", bytes, tags: [new string('x', AttachmentRules.MaxTagLength + 1)])));

        await Assert.ThrowsAsync<ValidationException>(() =>
            AsAdminAsync((scope, owner) =>
                Upload(scope, owner, "a.pdf", bytes, title: new string('n', AttachmentRules.MaxTitleLength + 1))));
    }

    [Fact]
    public async Task Editing_changes_the_title_and_labels_and_leaving_labels_out_keeps_them()
    {
        var bytes = TestFiles.WithSignature("a.pdf");

        var edited = await AsAdminAsync(async (scope, owner) =>
        {
            var created = await Upload(scope, owner, "a.pdf", bytes, title: "Staro", tags: ["a", "b"]);

            var kept = await scope.Send(new UpdateAttachmentCommand
            {
                Id = created.Id, Category = AttachmentCategory.Other, Title = "Novo",
            });
            Assert.Equal("Novo", kept.Title);
            Assert.Equal(["a", "b"], kept.Tags);

            var cleared = await scope.Send(new UpdateAttachmentCommand
            {
                Id = created.Id, Category = AttachmentCategory.Other, Title = null, Tags = [],
            });

            return cleared;
        });

        Assert.Null(edited.Title);
        Assert.Empty(edited.Tags);
    }

    [Fact]
    public async Task The_labels_in_use_are_suggested_most_used_first_without_repeats()
    {
        var word = $"oznaka-{Guid.NewGuid():N}"[..14];
        var bytes = TestFiles.WithSignature("a.pdf");

        var tags = await AsAdminAsync(async (scope, owner) =>
        {
            await Upload(scope, owner, "a.pdf", bytes, tags: [word]);
            await Upload(scope, owner, "b.pdf", bytes, tags: [word.ToUpperInvariant()]);
            await Upload(scope, owner, "c.pdf", bytes, tags: [word + "x"]);

            return await scope.Send(new GetAttachmentTagsQuery());
        });

        var own = tags.Where(t => t.StartsWith(word, StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Equal(2, own.Count);
        Assert.Equal(word, own[0], ignoreCase: true);
    }
}
