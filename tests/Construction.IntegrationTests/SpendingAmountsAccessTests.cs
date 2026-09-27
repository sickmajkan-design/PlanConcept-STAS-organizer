using System.Net;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The customer's rule: nothing in euro for anyone but the Super Admin and
/// whoever the Super Admin picks. A site role may record what was spent, but
/// reading the amounts back needs the finance grant, which no fixture account
/// has by default.
/// </summary>
[Collection(ApiCollection.Name)]
public class SpendingAmountsAccessTests
{
    private readonly ApiFixture _api;

    public SpendingAmountsAccessTests(ApiFixture api)
    {
        _api = api;
    }

    [Theory]
    [InlineData("/api/material-movements")]
    [InlineData("/api/vehicle-expenses")]
    public async Task Without_the_grant_the_amounts_are_refused_even_to_an_admin(string path)
    {
        foreach (var role in new[] { UserRole.Admin, UserRole.ProjectManager, UserRole.Foreman })
        {
            using var client = _api.ClientAs(role);

            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
        }
    }

    [Theory]
    [InlineData("/api/material-movements")]
    [InlineData("/api/vehicle-expenses")]
    public async Task The_super_admin_reads_them(string path)
    {
        using var client = _api.ClientAs(UserRole.SuperAdmin);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }
}
