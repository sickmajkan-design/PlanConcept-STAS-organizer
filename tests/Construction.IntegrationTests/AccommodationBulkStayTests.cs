using Construction.Application.Features.Accommodations.Stays;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Several people into one accommodation at once, each with their own dates and project: what is in order is
/// written, what is not is reported with the reason, and going over the number of beds is a warning.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AccommodationBulkStayTests : IntegrationTestBase
{
    public AccommodationBulkStayTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly Day = new(2030, 3, 1);

    private async Task<(Accommodation Place, List<Employee> People)> SeedAsync(int people, int? beds = null)
    {
        return await InScope(async scope =>
        {
            var place = await TestData.SeedAccommodationAsync(scope);
            place.Beds = beds;
            await scope.Db.SaveChangesAsync();

            var list = new List<Employee>();
            for (var i = 0; i < people; i++)
            {
                list.Add(await TestData.SeedEmployeeAsync(scope));
            }

            return (place, list);
        });
    }

    private Task<AccommodationStaysResultDto> SendAsync(Guid place, bool preview, params AccommodationStayRow[] rows) =>
        InScope(scope => scope.Send(new AddAccommodationStaysCommand { AccommodationId = place, Preview = preview, Stays = rows }));

    private static AccommodationStayRow Row(Employee e, DateOnly? start = null, DateOnly? end = null, Guid? project = null) =>
        new() { EmployeeId = e.Id, StartDate = start ?? Day, EndDate = end, ProjectId = project };

    [Fact]
    public async Task Everyone_in_order_is_written_each_with_their_own_dates_and_project()
    {
        var (place, people) = await SeedAsync(3, beds: 5);
        var (siteA, siteB) = await InScope(async scope => (await TestData.SeedProjectAsync(scope), await TestData.SeedProjectAsync(scope)));

        var result = await SendAsync(place.Id, false,
            Row(people[0], Day, project: siteA.Id),
            Row(people[1], Day.AddDays(5), Day.AddDays(20), siteB.Id),
            Row(people[2], Day));

        Assert.Equal(3, result.Saved);
        Assert.All(result.Rows, r => Assert.Equal("Ok", r.Status));

        var stays = await InScope(scope => scope.Db.AccommodationStays.Where(s => s.AccommodationId == place.Id).ToListAsync());
        Assert.Equal(3, stays.Count);
        Assert.Equal(siteB.Id, stays.Single(s => s.EmployeeId == people[1].Id).ProjectId);
        Assert.Equal(Day.AddDays(20), stays.Single(s => s.EmployeeId == people[1].Id).EndDate);
    }

    [Fact]
    public async Task A_preview_writes_nothing()
    {
        var (place, people) = await SeedAsync(2);

        var result = await SendAsync(place.Id, true, Row(people[0]), Row(people[1]));

        Assert.Equal(0, result.Saved);
        Assert.Equal(2, result.PeakOccupancy);
        Assert.Empty(await InScope(scope => scope.Db.AccommodationStays.Where(s => s.AccommodationId == place.Id).ToListAsync()));
    }

    [Fact]
    public async Task Someone_who_already_lives_elsewhere_is_reported_and_the_others_are_still_written()
    {
        var (place, people) = await SeedAsync(2);
        var other = await InScope(scope => TestData.SeedAccommodationAsync(scope, "Druga ulica 5"));
        await InScope(async scope =>
        {
            scope.Db.AccommodationStays.Add(new AccommodationStay { AccommodationId = other.Id, EmployeeId = people[0].Id, StartDate = Day.AddDays(-10) });
            await scope.Db.SaveChangesAsync();
        });

        var result = await SendAsync(place.Id, false, Row(people[0]), Row(people[1]));

        Assert.Equal(1, result.Saved);
        var refused = result.Rows.Single(r => r.EmployeeId == people[0].Id);
        Assert.Equal("Conflict", refused.Status);
        Assert.Equal("AlreadyHoused", refused.Reason);
        Assert.Equal("Druga ulica 5", refused.Place);
        Assert.Equal("Ok", result.Rows.Single(r => r.EmployeeId == people[1].Id).Status);
    }

    [Fact]
    public async Task Going_over_the_beds_is_a_warning_and_still_saves()
    {
        var (place, people) = await SeedAsync(3, beds: 2);

        var result = await SendAsync(place.Id, false, Row(people[0]), Row(people[1]), Row(people[2]));

        Assert.Equal(3, result.Saved);
        Assert.Equal(3, result.PeakOccupancy);
        Assert.Equal(2, result.Beds);
        Assert.All(result.Rows, r => Assert.Equal("OverCapacity", r.Status));
    }

    [Fact]
    public async Task People_who_never_share_a_day_do_not_go_over_the_beds()
    {
        var (place, people) = await SeedAsync(2, beds: 1);

        var result = await SendAsync(place.Id, true,
            Row(people[0], Day, Day.AddDays(9)),
            Row(people[1], Day.AddDays(10)));

        Assert.Equal(1, result.PeakOccupancy);
        Assert.All(result.Rows, r => Assert.Equal("Ok", r.Status));
    }

    [Fact]
    public async Task The_same_person_twice_in_one_batch_is_written_once()
    {
        var (place, people) = await SeedAsync(1);

        var result = await SendAsync(place.Id, false, Row(people[0]), Row(people[0]));

        Assert.Equal(1, result.Saved);
        Assert.Equal("ListedTwice", result.Rows[1].Reason);
    }

    [Fact]
    public async Task An_unknown_person_or_a_stay_that_ends_before_it_starts_is_refused_with_a_reason()
    {
        var (place, people) = await SeedAsync(1);

        var result = await SendAsync(place.Id, true,
            new AccommodationStayRow { EmployeeId = Guid.NewGuid(), StartDate = Day },
            Row(people[0], Day, Day.AddDays(-1)));

        Assert.Equal("EmployeeNotFound", result.Rows[0].Reason);
        Assert.Equal("EndBeforeStart", result.Rows[1].Reason);
    }
}
