using Construction.Application.Features.Absences;

namespace Construction.UnitTests;

/// <summary>
/// The customer's leave rules (26–27 Sept 2026): working days, 20 days a year, pro rata in the
/// year of starting, unused days carried over and lost on 1 June.
/// </summary>
public class LeaveCalculatorTests
{
    private static readonly IReadOnlySet<DateOnly> NoHolidays = new HashSet<DateOnly>();

    private static readonly IReadOnlyDictionary<int, int> NoAdjustments = new Dictionary<int, int>();

    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    /// <summary>Consecutive dates, weekends included; the calculator only counts what it is given.</summary>
    private static List<DateOnly> Days(int y, int m, int d, int count) =>
        Enumerable.Range(0, count).Select(i => D(y, m, d).AddDays(i)).ToList();

    // -------------------------------------------------------- working days

    [Fact]
    public void A_working_week_is_five_days_and_a_weekend_is_none()
    {
        // 28 Sept 2026 is a Monday.
        Assert.Equal(5, LeaveCalculator.WorkingDays(D(2026, 9, 28), D(2026, 10, 4), NoHolidays));
        Assert.Equal(0, LeaveCalculator.WorkingDays(D(2026, 10, 3), D(2026, 10, 4), NoHolidays));
    }

    [Fact]
    public void A_public_holiday_is_not_a_leave_day()
    {
        var holidays = new HashSet<DateOnly> { D(2026, 9, 30) };

        Assert.Equal(4, LeaveCalculator.WorkingDays(D(2026, 9, 28), D(2026, 10, 2), holidays));
    }

    [Fact]
    public void A_holiday_on_a_weekend_takes_nothing_off_twice()
    {
        var holidays = new HashSet<DateOnly> { D(2026, 10, 3) };

        Assert.Equal(5, LeaveCalculator.WorkingDays(D(2026, 9, 28), D(2026, 10, 4), holidays));
    }

    [Fact]
    public void One_day_of_leave_is_one_day_and_a_reversed_range_is_none()
    {
        Assert.Equal(1, LeaveCalculator.WorkingDays(D(2026, 9, 28), D(2026, 9, 28), NoHolidays));
        Assert.Equal(0, LeaveCalculator.WorkingDays(D(2026, 9, 29), D(2026, 9, 28), NoHolidays));
    }

    [Fact]
    public void Leave_across_new_year_only_counts_the_days_in_the_window()
    {
        // Mon 28 Dec 2026 to Fri 1 Jan 2027: four working days in 2026 (Thu 31 Dec counts, Fri 1 Jan is 2027).
        var inYear = LeaveCalculator.WorkingDaysWithin(
            D(2026, 12, 28), D(2027, 1, 1), D(2026, 1, 1), D(2026, 12, 31), NoHolidays).ToList();

        Assert.Equal(4, inYear.Count);
        Assert.All(inYear, d => Assert.Equal(2026, d.Year));
    }

    // ------------------------------------------------------------ entitlement

    [Theory]
    [InlineData(2020, 1, 1, 2026, 20)] // employed before the year: the full right
    [InlineData(2026, 1, 1, 2026, 20)] // started on the first day of the year
    [InlineData(2026, 7, 1, 2026, 10)] // six full months
    [InlineData(2026, 7, 15, 2026, 8)] // five full months: 8.33 rounds down
    [InlineData(2026, 12, 1, 2026, 2)] // one month
    [InlineData(2026, 12, 15, 2026, 0)] // no full month left
    [InlineData(2027, 1, 1, 2026, 0)]  // not employed yet
    public void The_right_is_pro_rata_in_the_year_of_starting(int y, int m, int d, int year, int expected)
    {
        Assert.Equal(expected, LeaveCalculator.Entitlement(20, D(y, m, d), year));
    }

    [Theory]
    [InlineData(25, 7, 1, 13)] // 12.5 rounds up
    [InlineData(21, 11, 1, 4)] // 3.5 rounds up
    [InlineData(20, 4, 1, 15)] // exactly 15
    public void A_half_day_rounds_up(int annual, int startMonth, int startDay, int expected)
    {
        Assert.Equal(expected, LeaveCalculator.Entitlement(annual, D(2026, startMonth, startDay), 2026));
    }

    // ---------------------------------------------------------------- carry-over

    [Fact]
    public void Unused_days_carry_into_the_next_year()
    {
        // 2025: 20 days, 12 taken in October (after the carry expiry, which is not a concern in 2025).
        var taken = Days(2025, 10, 6, 12);

        var standing = LeaveCalculator.Standing(20, D(2020, 1, 1), 2026, NoAdjustments, taken);

        Assert.Equal(8, standing.CarryIn);
        Assert.Equal(20, standing.Entitlement);
    }

    [Fact]
    public void Carried_days_are_used_first_and_lost_after_the_first_of_june()
    {
        var taken = Days(2025, 10, 6, 12)      // leaves 8 carried into 2026
            .Concat(Days(2026, 3, 2, 5))       // 5 days before 1 June: drawn from the carried 8
            .Concat(Days(2026, 7, 6, 3))       // 3 days after: drawn from the year's own right
            .ToList();

        var standing = LeaveCalculator.Standing(20, D(2020, 1, 1), 2026, NoAdjustments, taken);

        Assert.Equal(8, standing.CarryIn);
        Assert.Equal(5, standing.CarryInUsed);
        Assert.Equal(8, standing.UsedDays);

        // Before the expiry: 20 + 8 - 8. After: the 3 unused carried days are gone.
        Assert.Equal(20, LeaveCalculator.Remaining(standing, D(2026, 5, 31)));
        Assert.Equal(17, LeaveCalculator.Remaining(standing, D(2026, 6, 1)));
        Assert.Equal(0, LeaveCalculator.CarryExpired(standing, D(2026, 5, 31)));
        Assert.Equal(3, LeaveCalculator.CarryExpired(standing, D(2026, 6, 1)));
    }

    [Fact]
    public void Only_one_years_unused_days_carry_never_a_pile_from_older_years()
    {
        // Nothing is taken in 2025 or 2026: 2025's unused 20 carry into 2026, but they expire on
        // 1 June 2026, so 2027 starts with only what 2026's own right leaves, not 40.
        var standing = LeaveCalculator.Standing(20, D(2020, 1, 1), 2027, NoAdjustments, []);

        Assert.Equal(20, standing.CarryIn);
    }

    [Fact]
    public void Carried_days_come_off_first_so_taking_a_lot_early_loses_nothing()
    {
        var taken = Days(2025, 10, 6, 12)
            .Concat(Days(2026, 2, 2, 30))
            .ToList();

        var standing = LeaveCalculator.Standing(20, D(2020, 1, 1), 2026, NoAdjustments, taken);

        Assert.Equal(8, standing.CarryInUsed);
        Assert.Equal(0, standing.CarryOut);
        Assert.Equal(-2, LeaveCalculator.Remaining(standing, D(2026, 3, 1))); // 20 + 8 - 30: over the right, visible
    }

    [Fact]
    public void Nothing_carries_from_before_the_person_started()
    {
        var standing = LeaveCalculator.Standing(20, D(2026, 3, 1), 2026, NoAdjustments, []);

        Assert.Equal(0, standing.CarryIn);
        Assert.Equal(17, standing.Entitlement); // March to December: ten months, 16.67 rounds up
    }

    [Fact]
    public void A_person_started_last_year_carries_only_their_pro_rata_days()
    {
        // Started 1 July 2025: 10 days that year, none taken, so 10 carry into 2026.
        var standing = LeaveCalculator.Standing(20, D(2025, 7, 1), 2026, NoAdjustments, []);

        Assert.Equal(10, standing.CarryIn);
        Assert.Equal(20, standing.Entitlement);
    }

    // ------------------------------------------------------------- corrections

    [Fact]
    public void A_correction_adds_to_or_takes_from_the_years_right()
    {
        var plus = LeaveCalculator.Standing(20, D(2020, 1, 1), 2026, new Dictionary<int, int> { [2026] = 3 }, []);
        var minus = LeaveCalculator.Standing(20, D(2020, 1, 1), 2026, new Dictionary<int, int> { [2026] = -4 }, []);

        Assert.Equal(3, plus.Adjustments);
        Assert.Equal(-4, minus.Adjustments);
        Assert.Equal(20 + 20 + 3, LeaveCalculator.Remaining(plus, D(2026, 1, 2)));
        Assert.Equal(20 + 20 - 4, LeaveCalculator.Remaining(minus, D(2026, 1, 2)));
    }

    [Fact]
    public void A_correction_made_as_a_starting_balance_carries_on_like_any_right()
    {
        // 15 days brought over from before the system, entered as a correction for 2025.
        var standing = LeaveCalculator.Standing(
            20, D(2025, 1, 1), 2026, new Dictionary<int, int> { [2025] = 15 }, []);

        Assert.Equal(35, standing.CarryIn); // 20 + 15, none taken
    }

    [Fact]
    public void Leave_taken_in_other_years_does_not_count_against_this_one()
    {
        var taken = Days(2025, 3, 3, 4).Concat(Days(2027, 3, 1, 4)).ToList();

        var standing = LeaveCalculator.Standing(20, D(2020, 1, 1), 2026, NoAdjustments, taken);

        Assert.Equal(0, standing.UsedDays);
    }
}
