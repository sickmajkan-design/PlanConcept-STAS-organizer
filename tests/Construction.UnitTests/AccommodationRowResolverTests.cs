using Construction.Application.Features.Accommodations.Import;
using Xunit;

namespace Construction.UnitTests;

public class AccommodationRowResolverTests
{
    private static readonly DateOnly Today = new(2026, 9, 20);
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Marko = Guid.NewGuid();
    private static readonly Guid Site = Guid.NewGuid();
    private static readonly Guid Existing = Guid.NewGuid();

    private static readonly Dictionary<string, (Guid Id, string Name)> Employees = new()
    {
        ["e-1"] = (Ana, "Ana Anić"),
        ["ana anić"] = (Ana, "Ana Anić"),
        ["e-2"] = (Marko, "Marko Marković"),
    };

    private static IReadOnlyList<AccommodationImportRowResult> Resolve(
        string[][] rows,
        IReadOnlyCollection<KnownStay>? stays = null) =>
        AccommodationRowResolver.Resolve(
            rows.Select(r => (IReadOnlyList<string>)r).ToList(),
            new Dictionary<string, Guid> { ["ulica 5"] = Existing },
            Employees,
            new Dictionary<string, Guid> { ["most"] = Site },
            stays ?? [],
            Today);

    private static readonly string[] Header =
        ["Adresa", "Naziv", "Tip", "Kirija", "Radnik", "Useljenje", "Iseljenje", "Gradilište"];

    [Fact]
    public void A_new_address_with_two_people_creates_one_accommodation_and_two_stays()
    {
        var rows = Resolve([
            Header,
            ["Ulica 1", "Stan 1", "Stan", "450,50", "E-1", "01.09.2026", "", "Most"],
            ["ulica  1", "", "", "", "E-2", "01.09.2026", "", ""]]);

        Assert.All(rows, r => Assert.True(r.WillImport));
        Assert.Single(rows, r => r.CreatesAccommodation);
        Assert.Equal(2, rows.Count(r => r.CreatesStay));
        Assert.Equal(450.50m, rows[0].MonthlyRent);
        Assert.Equal(Site, rows[0].ProjectId);
    }

    [Fact]
    public void A_person_already_living_elsewhere_in_those_dates_is_a_conflict()
    {
        var rows = Resolve(
            [Header, ["Ulica 1", "", "", "", "E-1", "05.09.2026", "", ""]],
            [new KnownStay(Ana, Guid.NewGuid(), new DateOnly(2026, 8, 1), null)]);

        Assert.Equal(AccommodationImportRowStatus.StayConflict, rows[0].Status);
    }

    [Fact]
    public void Running_the_same_list_again_adds_nothing()
    {
        var rows = Resolve(
            [Header, ["Ulica 5", "", "", "", "E-1", "01.09.2026", "", ""]],
            [new KnownStay(Ana, Existing, new DateOnly(2026, 9, 1), null)]);

        Assert.Equal(AccommodationImportRowStatus.AlreadyImported, rows[0].Status);
    }

    [Fact]
    public void An_existing_address_without_a_person_adds_nothing()
    {
        var rows = Resolve([Header, ["Ulica 5", "", "", "", "", "", "", ""]]);

        Assert.Equal(AccommodationImportRowStatus.AlreadyImported, rows[0].Status);
    }

    [Fact]
    public void Unknown_people_projects_and_bad_values_are_reported_per_row()
    {
        var rows = Resolve([
            Header,
            ["", "", "", "", "", "", "", ""],
            ["Ulica 2", "", "Dvorac", "", "", "", "", ""],
            ["Ulica 3", "", "", "abc", "", "", "", ""],
            ["Ulica 4", "", "", "", "E-9", "", "", ""],
            ["Ulica 6", "", "", "", "", "", "", "Nepoznato"],
            ["Ulica 7", "", "", "", "", "10.09.2026", "01.09.2026", ""]]);

        Assert.Equal(
            [
                AccommodationImportRowStatus.MissingAddress,
                AccommodationImportRowStatus.InvalidType,
                AccommodationImportRowStatus.InvalidNumber,
                AccommodationImportRowStatus.UnknownEmployee,
                AccommodationImportRowStatus.UnknownProject,
                AccommodationImportRowStatus.InvalidDate
            ],
            rows.Select(r => r.Status).ToArray());
    }
}
