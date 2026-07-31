using Construction.Application.Features.Locations.Commands.ReportLocations;
using Construction.UnitTests.Fakes;

namespace Construction.UnitTests.Validation;

public class ReportLocationsValidatorTests
{
    private readonly FixedDateTimeProvider _clock = new();
    private readonly ReportLocationsCommandValidator _validator;

    public ReportLocationsValidatorTests()
    {
        _validator = new ReportLocationsCommandValidator(_clock);
    }

    private LocationPing Ping(double lat = 45.8131, double lng = 15.9775, double? accuracy = 7.5) =>
        new()
        {
            Latitude = lat,
            Longitude = lng,
            Accuracy = accuracy,
            Timestamp = _clock.UtcNow.AddMinutes(-1)
        };

    private ReportLocationsCommand Batch(params LocationPing[] pings) => new() { Pings = pings };

    [Fact]
    public void Accepts_a_batch_of_fixes()
    {
        ValidationAssert.Valid(_validator, Batch(Ping(), Ping(45.8132, 15.9778)));
    }

    [Fact]
    public void Rejects_an_empty_batch()
    {
        ValidationAssert.Invalid(_validator, Batch(), "Pings");
    }

    [Fact]
    public void Accepts_a_full_offline_buffer_of_120_fixes()
    {
        // Matches the mobile app's buffer cap; one more must be refused.
        var full = Enumerable.Range(0, 120).Select(_ => Ping()).ToArray();

        ValidationAssert.Valid(_validator, Batch(full));
        ValidationAssert.Invalid(_validator, Batch([.. full, Ping()]), "Pings");
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Rejects_a_latitude_outside_range(double latitude)
    {
        ValidationAssert.Invalid(_validator, Batch(Ping(lat: latitude)), "Pings[0].Latitude");
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Rejects_a_longitude_outside_range(double longitude)
    {
        ValidationAssert.Invalid(_validator, Batch(Ping(lng: longitude)), "Pings[0].Longitude");
    }

    [Fact]
    public void Rejects_a_negative_accuracy()
    {
        ValidationAssert.Invalid(_validator, Batch(Ping(accuracy: -1)), "Pings[0].Accuracy");
    }

    [Fact]
    public void Accepts_a_fix_with_no_accuracy_reported()
    {
        ValidationAssert.Valid(_validator, Batch(Ping(accuracy: null)));
    }

    [Fact]
    public void Rejects_a_timestamp_beyond_the_allowed_clock_skew()
    {
        var tooFarAhead = Ping() with { Timestamp = _clock.UtcNow.AddMinutes(6) };

        ValidationAssert.Invalid(_validator, Batch(tooFarAhead), "Pings[0].Timestamp");
    }

    [Fact]
    public void Tolerates_a_device_clock_running_slightly_fast()
    {
        // Phones drift; a few minutes ahead must not cost the crew their data.
        var slightlyAhead = Ping() with { Timestamp = _clock.UtcNow.AddMinutes(4) };

        ValidationAssert.Valid(_validator, Batch(slightlyAhead));
    }

    [Fact]
    public void Reports_the_offending_ping_when_only_one_in_the_batch_is_bad()
    {
        var batch = Batch(Ping(), Ping(lat: 999), Ping());

        ValidationAssert.Invalid(_validator, batch, "Pings[1].Latitude");
    }
}
