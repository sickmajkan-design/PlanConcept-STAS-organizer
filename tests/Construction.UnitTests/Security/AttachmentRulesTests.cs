using Construction.Application.Features.Attachments;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Security;

public class AttachmentRulesTests
{
    // ---- what may be uploaded -------------------------------------------

    [Theory]
    [InlineData("ugovor.pdf", "application/pdf")]
    [InlineData("slika.JPG", "image/jpeg")]
    [InlineData("nacrt.PnG", "image/png")]
    public void Resolves_the_content_type_from_the_extension(string name, string expected)
    {
        // Case-insensitively: a phone camera writing IMG_0001.JPG must work.
        Assert.Equal(expected, AttachmentRules.ResolveContentType(name));
    }

    [Theory]
    [InlineData("payload.exe")]
    [InlineData("script.sh")]
    [InlineData("archive.zip")]
    [InlineData("noextension")]
    [InlineData("trick.pdf.exe")]
    public void Refuses_a_type_that_is_not_on_the_allow_list(string name)
    {
        // Including the double extension, where only the last one counts.
        Assert.Null(AttachmentRules.ResolveContentType(name));
    }

    // ---- names -----------------------------------------------------------

    [Fact]
    public void Strips_a_path_off_a_file_name()
    {
        // Browsers on some platforms send the full path.
        Assert.Equal("ugovor.pdf", AttachmentRules.SanitiseFileName("C:\\Users\\ana\\ugovor.pdf"));
        Assert.Equal("ugovor.pdf", AttachmentRules.SanitiseFileName("/home/ana/ugovor.pdf"));
    }

    [Fact]
    public void Strips_characters_that_would_break_out_of_a_header()
    {
        // The name travels in Content-Disposition, where a newline or a quote
        // would let an uploader append a header of their own.
        var cleaned = AttachmentRules.SanitiseFileName("bad\r\nX-Injected: 1\".pdf");

        Assert.DoesNotContain('\r', cleaned);
        Assert.DoesNotContain('\n', cleaned);
        Assert.DoesNotContain('"', cleaned);
    }

    [Fact]
    public void Never_returns_an_empty_name()
    {
        Assert.False(string.IsNullOrWhiteSpace(AttachmentRules.SanitiseFileName("   ")));
    }

    // ---- storage keys ----------------------------------------------------

    [Fact]
    public void Builds_a_key_from_ids_rather_than_from_the_name()
    {
        // The name is attacker-controlled; the key must not be.
        var key = AttachmentRules.BuildStorageKey(
            AttachmentOwnerType.Employee,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "../../etc/passwd.pdf");

        Assert.StartsWith("employees/", key);
        Assert.DoesNotContain("..", key);
        Assert.DoesNotContain("passwd", key);
        Assert.EndsWith(".pdf", key);
    }

    [Fact]
    public void Gives_two_uploads_of_the_same_name_different_keys()
    {
        var ownerId = Guid.NewGuid();

        var first = AttachmentRules.BuildStorageKey(
            AttachmentOwnerType.Project, ownerId, Guid.NewGuid(), "nacrt.pdf");
        var second = AttachmentRules.BuildStorageKey(
            AttachmentOwnerType.Project, ownerId, Guid.NewGuid(), "nacrt.pdf");

        Assert.NotEqual(first, second);
    }

    // ---- who may read ----------------------------------------------------

    [Theory]
    [InlineData(UserRole.SuperAdmin, true)]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.ProjectManager, false)]
    [InlineData(UserRole.Foreman, false)]
    [InlineData(UserRole.Worker, false)]
    public void Employee_files_stop_at_admin(UserRole role, bool expected)
    {
        // Contracts and occupational medicals are the sensitive ones.
        Assert.Equal(
            expected,
            AttachmentRules.CanRead(role, AttachmentOwnerType.Employee));
    }

    [Theory]
    [InlineData(UserRole.ProjectManager, true)]
    [InlineData(UserRole.Foreman, true)]
    [InlineData(UserRole.Worker, false)]
    public void Site_files_reach_the_foreman(UserRole role, bool expected)
    {
        Assert.Equal(
            expected,
            AttachmentRules.CanRead(role, AttachmentOwnerType.Project));
    }

    [Fact]
    public void An_absent_role_reads_nothing()
    {
        // A token shaped in a way this build did not expect gets the least
        // access, not the most.
        Assert.False(AttachmentRules.CanRead(null, AttachmentOwnerType.Project));
        Assert.False(AttachmentRules.CanRead(null, AttachmentOwnerType.Employee));
    }

    [Fact]
    public void An_employee_reaches_their_own_file_and_no_other()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();

        Assert.True(AttachmentRules.CanReadOwnEmployeeFiles(
            mine, new Attachment { EmployeeId = mine }));

        Assert.False(AttachmentRules.CanReadOwnEmployeeFiles(
            mine, new Attachment { EmployeeId = theirs }));

        // An account with no employee record matches nothing, rather than
        // matching every attachment whose EmployeeId is also null.
        Assert.False(AttachmentRules.CanReadOwnEmployeeFiles(
            null, new Attachment { EmployeeId = null, ProjectId = Guid.NewGuid() }));
    }

    // ---- who may write ---------------------------------------------------

    [Fact]
    public void A_worker_may_add_a_site_photo()
    {
        Assert.True(AttachmentRules.CanUpload(
            UserRole.Worker, AttachmentOwnerType.Project, AttachmentCategory.Photo));
    }

    [Theory]
    [InlineData(AttachmentOwnerType.Project, AttachmentCategory.Contract)]
    [InlineData(AttachmentOwnerType.Employee, AttachmentCategory.Photo)]
    [InlineData(AttachmentOwnerType.Vehicle, AttachmentCategory.Photo)]
    public void A_worker_may_add_nothing_else(
        AttachmentOwnerType ownerType,
        AttachmentCategory category)
    {
        // Self-certification in particular: a worker filing their own
        // certificate would be signing off their own competence.
        Assert.False(AttachmentRules.CanUpload(UserRole.Worker, ownerType, category));
    }

    [Fact]
    public void A_foreman_may_add_anything()
    {
        Assert.True(AttachmentRules.CanUpload(
            UserRole.Foreman, AttachmentOwnerType.Employee, AttachmentCategory.Contract));
    }

    [Theory]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.ProjectManager, false)]
    [InlineData(UserRole.Foreman, false)]
    public void Deleting_stops_at_admin(UserRole role, bool expected)
    {
        // Deletion removes the bytes and is not reversible.
        Assert.Equal(expected, AttachmentRules.CanDelete(role));
    }
}
