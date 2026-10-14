using Construction.Application.Features.Overview;
using Construction.UnitTests.Fakes;

namespace Construction.UnitTests.Validation;

public class OverviewValidatorTests
{
    private readonly GetOverviewQueryValidator _validator = new();

    private static GetOverviewQuery Period(DateOnly from, DateOnly to) => new() { From = from, To = to };

    [Fact]
    public void Accepts_a_month()
    {
        ValidationAssert.Valid(_validator, Period(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));
    }

    [Fact]
    public void Rejects_a_missing_start_date()
    {
        // An omitted date binds to 0001-01-01; stepping a day back from it threw a 500 in the handler.
        ValidationAssert.Invalid(_validator, Period(default, new DateOnly(2026, 10, 31)), "From");
    }

    [Fact]
    public void Rejects_a_missing_end_date()
    {
        ValidationAssert.Invalid(_validator, Period(new DateOnly(2026, 10, 1), default), "To");
    }

    [Fact]
    public void Rejects_an_end_before_the_start()
    {
        ValidationAssert.Invalid(_validator, Period(new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 1)), "To");
    }

    [Fact]
    public void Rejects_a_period_longer_than_two_years()
    {
        ValidationAssert.Invalid(_validator, Period(new DateOnly(2020, 1, 1), new DateOnly(2026, 1, 1)), "To");
    }
}
