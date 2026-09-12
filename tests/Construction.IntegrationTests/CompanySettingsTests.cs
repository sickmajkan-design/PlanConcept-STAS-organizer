using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The platform's own company profile, over HTTP. Worth its own suite rather
/// than leaning on <see cref="ApiAuthorizationTests"/> alone: the point of
/// this feature is a genuine <c>[AllowAnonymous]</c> carve-out, and the thing
/// that needs proving is not just "anonymous gets in" but "anonymous gets in
/// and sees nothing beyond name and logo" — which only a real round trip
/// through the projection can show.
/// </summary>
[Collection(ApiCollection.Name)]
public class CompanySettingsTests
{
    private readonly ApiFixture _api;

    public CompanySettingsTests(ApiFixture api)
    {
        _api = api;
    }

    private static StringContent AsJson(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    [Fact]
    public async Task A_super_admin_can_set_the_profile_and_it_round_trips()
    {
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        var payload = new
        {
            name = "QATEST Company d.o.o.",
            address = "QATEST Address 1",
            taxId = "QATEST-PIB-1",
            registrationNumber = "QATEST-MB-1",
            vatNumber = "QATEST-VAT-1",
            phone = "+381 60 000 0000",
            email = "qatest@example.com"
        };

        var put = await superAdmin.PutAsync("/api/company-settings", AsJson(payload));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await superAdmin.GetAsync("/api/company-settings");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var body = await get.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("QATEST Company d.o.o.", body.GetProperty("name").GetString());
        Assert.Equal("QATEST Address 1", body.GetProperty("address").GetString());
        Assert.Equal("QATEST-PIB-1", body.GetProperty("taxId").GetString());
        Assert.Equal("QATEST-MB-1", body.GetProperty("registrationNumber").GetString());
        Assert.Equal("QATEST-VAT-1", body.GetProperty("vatNumber").GetString());
        Assert.Equal("+381 60 000 0000", body.GetProperty("phone").GetString());
        Assert.Equal("qatest@example.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task A_non_super_admin_is_forbidden_from_writing_but_can_read()
    {
        using var admin = _api.ClientAs(UserRole.Admin);

        var put = await admin.PutAsync(
            "/api/company-settings",
            AsJson(new { name = "Smuggled Name" }));

        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);

        var get = await admin.GetAsync("/api/company-settings");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task The_anonymous_branding_endpoint_answers_with_no_auth_header_at_all()
    {
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);
        await superAdmin.PutAsync(
            "/api/company-settings",
            AsJson(new
            {
                name = "QATEST Branding Co",
                address = "Sensitive Address",
                taxId = "Sensitive Tax Id",
                phone = "Sensitive Phone",
                email = "sensitive@example.com"
            }));

        using var anonymous = _api.AnonymousClient();
        // No Authorization header set at all — the whole point of the route.
        var response = await anonymous.GetAsync("/api/company-settings/branding");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<JsonElement>(raw);

        Assert.Equal("QATEST Branding Co", body.GetProperty("name").GetString());
        Assert.False(body.GetProperty("hasLogo").GetBoolean());

        // The response must not carry any of the sensitive fields — not null
        // values, not present at all, anywhere in the payload's raw text.
        Assert.DoesNotContain("Sensitive", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("address", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("taxId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phone", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("registrationNumber", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vatNumber", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_logo_round_trips_anonymously_and_404s_once_removed()
    {
        using var superAdmin = _api.ClientAs(UserRole.SuperAdmin);

        // A minimal valid 1x1 PNG.
        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "logo.png");

        var upload = await superAdmin.PostAsync("/api/company-settings/logo", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        using var anonymous = _api.AnonymousClient();

        var logoResponse = await anonymous.GetAsync("/api/company-settings/logo");
        Assert.Equal(HttpStatusCode.OK, logoResponse.StatusCode);
        Assert.Equal("image/png", logoResponse.Content.Headers.ContentType?.MediaType);

        var downloaded = await logoResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(pngBytes, downloaded);

        var branding = await anonymous.GetAsync("/api/company-settings/branding");
        var body = await branding.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("hasLogo").GetBoolean());

        var delete = await superAdmin.DeleteAsync("/api/company-settings/logo");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var anonymousAfterDelete = _api.AnonymousClient();
        var afterDelete = await anonymousAfterDelete.GetAsync("/api/company-settings/logo");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task A_non_super_admin_cannot_upload_or_delete_the_logo()
    {
        using var admin = _api.ClientAs(UserRole.Admin);

        var pngBytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "logo.png");

        var upload = await admin.PostAsync("/api/company-settings/logo", form);
        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);

        var delete = await admin.DeleteAsync("/api/company-settings/logo");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}
