using Construction.Application.Features.SystemStatus;

namespace Construction.UnitTests.SystemStatus;

public class ServerMetricsParserTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Samples_become_hourly_averages_and_load_is_divided_by_the_cores()
    {
        var log =
            "2026-11-14T10:05:00Z|1.0|2|40|50\n" +
            "2026-11-14T10:35:00Z|3.0|2|60|52\n" +
            "2026-11-14T11:05:00Z|0.2|2|50|51\n";

        var points = ServerMetricsParser.Parse(log, Now);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc), points[0].AtUtc);
        Assert.Equal(100, points[0].LoadPercent); // (1.0 + 3.0) / 2 samples / 2 cores
        Assert.Equal(50, points[0].MemoryPercent);
        Assert.Equal(51, points[0].DiskPercent);
        Assert.Equal(10, points[1].LoadPercent);
    }

    [Fact]
    public void Old_future_and_broken_lines_are_left_out()
    {
        var log =
            "2026-11-01T10:00:00Z|1|2|40|50\n" +   // older than seven days
            "2026-11-20T10:00:00Z|1|2|40|50\n" +   // in the future
            "not a line\n" +
            "2026-11-14T10:00:00Z|1|0|40|50\n" +   // zero cores
            "2026-11-14T10:00:00Z|1|2|40|50\n";

        Assert.Single(ServerMetricsParser.Parse(log, Now));
        Assert.Empty(ServerMetricsParser.Parse(null, Now));
    }

    [Fact]
    public void A_disk_that_gains_a_point_a_day_is_read_as_one_point_a_day()
    {
        var points = Enumerable.Range(0, 72)
            .Select(hour => new MetricPointDto { AtUtc = Now.AddHours(hour - 72), DiskPercent = 50 + hour / 24.0 })
            .ToList();

        var slope = ServerMetricsParser.DiskPercentPerDay(points);

        Assert.NotNull(slope);
        Assert.Equal(1.0, slope.Value, 3);
    }

    [Fact]
    public void Under_a_day_of_data_gives_no_trend()
    {
        var points = Enumerable.Range(0, 5)
            .Select(hour => new MetricPointDto { AtUtc = Now.AddHours(hour), DiskPercent = 50 + hour })
            .ToList();

        Assert.Null(ServerMetricsParser.DiskPercentPerDay(points));
        Assert.Null(ServerMetricsParser.DiskPercentPerDay([]));
    }
}
