using Construction.Application.Features.Finance;

namespace Construction.UnitTests;

/// <summary>
/// How a period is cut into bars. The bars must cover the period exactly —
/// no gap, no overlap — or the chart's total would drift from the headline.
/// </summary>
public class FinanceBucketsTests
{
    private static void AssertCoversExactly(DateOnly from, DateOnly to, List<(DateOnly From, DateOnly To)> buckets)
    {
        Assert.Equal(from, buckets[0].From);
        Assert.Equal(to, buckets[^1].To);

        for (var i = 1; i < buckets.Count; i++)
        {
            Assert.Equal(buckets[i - 1].To.AddDays(1), buckets[i].From);
        }
    }

    [Fact]
    public void Days_are_one_bar_each()
    {
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);

        var buckets = FinanceBuckets.Build(from, to, FinanceGranularity.Day);

        Assert.Equal(30, buckets.Count);
        AssertCoversExactly(from, to, buckets);
    }

    [Fact]
    public void Weeks_end_on_sunday_and_the_first_and_last_are_clipped()
    {
        // 2026-09-02 is a Wednesday; 2026-09-24 a Thursday.
        var from = new DateOnly(2026, 9, 2);
        var to = new DateOnly(2026, 9, 24);

        var buckets = FinanceBuckets.Build(from, to, FinanceGranularity.Week);

        Assert.Equal(new DateOnly(2026, 9, 6), buckets[0].To);
        Assert.Equal(DayOfWeek.Sunday, buckets[0].To.DayOfWeek);
        Assert.Equal(new DateOnly(2026, 9, 7), buckets[1].From);
        Assert.Equal(DayOfWeek.Monday, buckets[1].From.DayOfWeek);
        Assert.Equal(to, buckets[^1].To);
        AssertCoversExactly(from, to, buckets);
    }

    [Fact]
    public void A_period_that_starts_on_a_sunday_gives_that_day_its_own_week()
    {
        var sunday = new DateOnly(2026, 9, 6);

        var buckets = FinanceBuckets.Build(sunday, new DateOnly(2026, 9, 14), FinanceGranularity.Week);

        Assert.Equal((sunday, sunday), buckets[0]);
        Assert.Equal(new DateOnly(2026, 9, 7), buckets[1].From);
    }

    [Fact]
    public void Months_follow_the_calendar_including_a_leap_february()
    {
        var from = new DateOnly(2028, 1, 15);
        var to = new DateOnly(2028, 3, 10);

        var buckets = FinanceBuckets.Build(from, to, FinanceGranularity.Month);

        Assert.Equal(3, buckets.Count);
        Assert.Equal(new DateOnly(2028, 1, 31), buckets[0].To);
        Assert.Equal(new DateOnly(2028, 2, 29), buckets[1].To);
        AssertCoversExactly(from, to, buckets);
    }

    [Fact]
    public void A_single_day_is_a_single_bar_at_every_granularity()
    {
        var day = new DateOnly(2026, 9, 24);

        foreach (var granularity in Enum.GetValues<FinanceGranularity>())
        {
            Assert.Equal([(day, day)], FinanceBuckets.Build(day, day, granularity));
        }
    }
}
