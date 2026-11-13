using System.Net;
using System.Net.Http.Headers;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Text in an upload's form fields. PostgreSQL cannot store a NUL character, and a multipart form bypasses the JSON
/// converter that already refuses one, so the same refusal has to exist for forms.
/// </summary>
[Collection(ApiCollection.Name)]
public class UploadFormTests
{
    private readonly ApiFixture _api;

    public UploadFormTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<Guid> EmployeeAsync() =>
        await _api.InScope(async db =>
        {
            var employee = new Employee
            {
                EmployeeNumber = $"U-{Guid.NewGuid():N}"[..14],
                FirstName = "Upload",
                LastName = "Form",
                Position = "Monter",
                EmploymentDate = new DateOnly(2020, 1, 1),
                Status = EmployeeStatus.Active,
            };

            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            return employee.Id;
        });

    private static MultipartFormDataContent Form(Guid ownerId, string title)
    {
        var file = new ByteArrayContent(TestFiles.WithSignature("ugovor.pdf", "sadrzaj"));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        return new MultipartFormDataContent
        {
            { new StringContent("Employee"), "ownerType" },
            { new StringContent(ownerId.ToString()), "ownerId" },
            { new StringContent("Contract"), "category" },
            { new StringContent(title), "title" },
            { file, "file", "ugovor.pdf" },
        };
    }

    [Fact]
    public async Task A_nul_character_in_a_form_field_is_a_400_not_a_500()
    {
        var owner = await EmployeeAsync();
        using var admin = _api.ClientAs(UserRole.Admin);

        var response = await admin.PostAsync("/api/v1/attachments", Form(owner, "naslov\0skriven"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_ordinary_title_is_still_accepted()
    {
        var owner = await EmployeeAsync();
        using var admin = _api.ClientAs(UserRole.Admin);

        var response = await admin.PostAsync("/api/v1/attachments", Form(owner, "Ugovor o radu"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("fake.docx")]
    [InlineData("fake.xlsx")]
    public async Task A_file_that_only_starts_like_an_archive_is_a_409_not_a_500(string fileName)
    {
        var owner = await EmployeeAsync();
        using var admin = _api.ClientAs(UserRole.Admin);

        var file = new ByteArrayContent([.. "PK"u8, .. "MZ program"u8]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var form = new MultipartFormDataContent
        {
            { new StringContent("Employee"), "ownerType" },
            { new StringContent(owner.ToString()), "ownerId" },
            { new StringContent("Contract"), "category" },
            { file, "file", fileName },
        };

        var response = await admin.PostAsync("/api/v1/attachments", form);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
