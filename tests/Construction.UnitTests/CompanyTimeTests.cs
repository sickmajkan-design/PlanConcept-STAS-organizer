using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.UnitTests.Fakes;

namespace Construction.UnitTests;

/// <summary>
/// What a moment is on the company's calendar. The zone is named in each test (never set globally), so these
/// cannot disturb tests that run beside them on a UTC company clock.
/// </summary>
public class CompanyTimeTests
{
    private static readonly TimeZoneInfo Belgrade = TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade");

    [Fact]
    public void A_moment_after_midnight_in_belgrade_is_the_next_day_there_though_still_the_day_before_in_utc()
    {
        // 22:30 UTC on 18 October is 00:30 on the 19th in Belgrade (UTC+2).
        var instant = new DateTime(2026, 10, 18, 22, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 10, 19), CompanyTime.LocalDate(instant, Belgrade));
        Assert.Equal(new TimeOnly(0, 30), CompanyTime.LocalTimeOfDay(instant, Belgrade));
        Assert.Equal(new DateOnly(2026, 10, 18), CompanyTime.LocalDate(instant, TimeZoneInfo.Utc));
    }

    [Fact]
    public void The_offset_is_one_hour_in_winter_and_two_in_summer()
    {
        Assert.Equal(new DateOnly(2026, 1, 16), CompanyTime.LocalDate(new DateTime(2026, 1, 15, 23, 30, 0, DateTimeKind.Utc), Belgrade));
        Assert.Equal(new DateOnly(2026, 1, 15), CompanyTime.LocalDate(new DateTime(2026, 1, 15, 22, 30, 0, DateTimeKind.Utc), Belgrade));
        Assert.Equal(new DateOnly(2026, 7, 16), CompanyTime.LocalDate(new DateTime(2026, 7, 15, 22, 30, 0, DateTimeKind.Utc), Belgrade));
    }

    [Fact]
    public void A_day_starts_at_the_companys_own_midnight()
    {
        Assert.Equal(new DateTime(2026, 10, 18, 22, 0, 0, DateTimeKind.Utc), CompanyTime.StartOfDayUtc(new DateOnly(2026, 10, 19), Belgrade));
        Assert.Equal(new DateTime(2026, 1, 14, 23, 0, 0, DateTimeKind.Utc), CompanyTime.StartOfDayUtc(new DateOnly(2026, 1, 15), Belgrade));
    }

    [Fact]
    public void A_day_is_twenty_five_hours_long_when_the_clocks_go_back_and_twenty_three_when_they_go_forward()
    {
        var (backStart, backEnd) = CompanyTime.Window(new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 25), Belgrade);
        var (forwardStart, forwardEnd) = CompanyTime.Window(new DateOnly(2026, 3, 29), new DateOnly(2026, 3, 29), Belgrade);

        Assert.Equal(25, (backEnd - backStart).TotalHours);
        Assert.Equal(23, (forwardEnd - forwardStart).TotalHours);
    }

    [Fact]
    public void A_window_of_days_runs_to_the_midnight_after_the_last_one_and_each_instant_belongs_to_exactly_one_day()
    {
        var (start, end) = CompanyTime.Window(new DateOnly(2026, 10, 18), new DateOnly(2026, 10, 19), Belgrade);

        Assert.Equal(new DateTime(2026, 10, 17, 22, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 10, 19, 22, 0, 0, DateTimeKind.Utc), end);

        // Midnight itself is the first moment of the new day, not the last of the old one.
        var midnight = new DateTime(2026, 10, 18, 22, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 10, 19), CompanyTime.LocalDate(midnight, Belgrade));
        Assert.Equal(new DateOnly(2026, 10, 18), CompanyTime.LocalDate(midnight.AddTicks(-1), Belgrade));
    }

    [Fact]
    public void An_unknown_zone_falls_back_to_utc_and_says_so_instead_of_stopping_the_application()
    {
        var before = CompanyTime.ZoneId;

        try
        {
            CompanyTime.Configure("Not/AZone", out var problem);

            Assert.NotNull(problem);
            Assert.Equal(TimeZoneInfo.Utc.Id, CompanyTime.ZoneId);
        }
        finally
        {
            CompanyTime.Configure(before, out _);
        }
    }

    [Fact]
    public void Today_is_the_companys_day_for_any_clock_that_does_not_say_otherwise()
    {
        // The interface's own default: the date of the instant on the company's clock. The unit tests run it on UTC.
        IDateTimeProvider clock = new FixedDateTimeProvider(new DateTime(2026, 10, 18, 22, 30, 0, DateTimeKind.Utc));

        Assert.Equal(CompanyTime.LocalDate(clock.UtcNow), clock.Today);
    }
}
