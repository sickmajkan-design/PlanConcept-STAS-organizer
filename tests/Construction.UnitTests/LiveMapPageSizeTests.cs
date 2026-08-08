using System.Text.RegularExpressions;
using Construction.Application.Features.Locations.Queries.GetCurrentLocations;

namespace Construction.UnitTests;

/// <summary>
/// The live map's page ceiling, which is written down in two languages.
/// </summary>
/// <remarks>
/// <para>
/// The server refuses a page larger than <c>MaxPageSize</c>, and the admin
/// panel asks for exactly that number because a map wants every marker it can
/// get in one request. The two constants cannot be shared across the language
/// boundary, so they can drift — and both directions fail quietly.
/// </para>
/// <para>
/// Raise the server's ceiling and forget the client, and the map silently
/// keeps drawing the old, smaller number of people while reporting a larger
/// total. Lower it and forget the client, and every request is a 400: the map
/// is simply blank, on a screen whose empty state already means "nobody has
/// reported yet". Neither is caught by a type checker or by either suite on
/// its own.
/// </para>
/// </remarks>
public class LiveMapPageSizeTests
{
    [Fact]
    public void The_admin_panel_asks_for_exactly_the_page_the_API_will_serve()
    {
        var source = File.ReadAllText(CorsOriginsTests.FindRepositoryFile(
            "src", "construction_admin", "src", "api", "locations.ts"));

        var declared = Regex.Match(source, @"MAP_PAGE_SIZE\s*=\s*(\d+)");

        Assert.True(
            declared.Success,
            "MAP_PAGE_SIZE is no longer declared in the admin locations client. "
            + "If it moved, move this test with it — the two numbers still have to agree.");

        Assert.Equal(
            GetCurrentLocationsQuery.MaxPageSize,
            int.Parse(declared.Groups[1].Value));
    }

    [Fact]
    public void The_default_page_is_smaller_than_the_ceiling_but_still_map_sized()
    {
        // A caller that sends no page size still gets a usable map, and the
        // ceiling stays available for the client that asks for it.
        var query = new GetCurrentLocationsQuery();

        Assert.InRange(query.PageSize, 100, GetCurrentLocationsQuery.MaxPageSize);
        Assert.Equal(1, query.PageNumber);
    }
}
