namespace Construction.Application.Features.Absences;

/// <summary>
/// The customer's rules for annual leave, as pure arithmetic: leave is counted in
/// working days, the right is pro rata in the year someone starts, and what is not
/// used carries into the next year but expires on 1 June.
/// </summary>
/// <remarks>
/// Nothing here reads the database, so every rule can be tested with plain dates.
/// </remarks>
public static class LeaveCalculator
{
    /// <summary>Carried-over days may be used until this day of June of the following year.</summary>
    public const int CarryOverExpiryMonth = 6;

    public const int CarryOverExpiryDay = 1;

    /// <summary>How many past years the carry-over chain looks back through.</summary>
    public const int MaxCarryYears = 5;

    /// <summary>The first day a carried-over day can no longer be used: 1 June of <paramref name="year"/>.</summary>
    public static DateOnly CarryOverExpiry(int year) => new(year, CarryOverExpiryMonth, CarryOverExpiryDay);

    /// <summary>Monday to Friday, and not a public holiday. Both ends are included.</summary>
    public static int WorkingDays(DateOnly from, DateOnly to, IReadOnlySet<DateOnly> holidays)
    {
        var count = 0;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day, holidays))
            {
                count++;
            }
        }

        return count;
    }

    public static bool IsWorkingDay(DateOnly day, IReadOnlySet<DateOnly> holidays) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(day);

    /// <summary>The working days of a stretch of leave that fall inside a year (or any other window).</summary>
    public static IEnumerable<DateOnly> WorkingDaysWithin(
        DateOnly start,
        DateOnly end,
        DateOnly windowStart,
        DateOnly windowEnd,
        IReadOnlySet<DateOnly> holidays)
    {
        var from = start > windowStart ? start : windowStart;
        var to = end < windowEnd ? end : windowEnd;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day, holidays))
            {
                yield return day;
            }
        }
    }

    /// <summary>
    /// The right for a calendar year. Full in every year after the first; in the year of
    /// starting, a twelfth for every full month (a start on the 1st counts that month),
    /// rounded to the nearest day with a half rounding up.
    /// </summary>
    public static int Entitlement(int annualDays, DateOnly employmentDate, int year)
    {
        if (employmentDate.Year > year)
        {
            return 0;
        }

        if (employmentDate.Year < year)
        {
            return annualDays;
        }

        var months = 12 - employmentDate.Month + (employmentDate.Day == 1 ? 1 : 0);

        return (int)Math.Round(annualDays * months / 12m, MidpointRounding.AwayFromZero);
    }

    /// <summary>One year's standing, before the next year's carry-over is derived from it.</summary>
    public sealed record YearStanding(
        int Year,
        int Entitlement,
        int Adjustments,
        int CarryIn,
        int CarryInUsed,
        int UsedDays,
        int UsedBeforeExpiry,
        int CarryOut);

    /// <summary>
    /// Works one year out. Days taken before the carry-over expires draw on it first, so
    /// nobody loses carried days they could have used; what is left of it is gone from the
    /// expiry date on, and only the rest of the year's own right carries forward.
    /// </summary>
    public static YearStanding Year(
        int year,
        int entitlement,
        int adjustments,
        int carryIn,
        IReadOnlyCollection<DateOnly> daysTaken)
    {
        var expiry = CarryOverExpiry(year);
        var used = daysTaken.Count(d => d.Year == year);
        var usedBeforeExpiry = daysTaken.Count(d => d.Year == year && d < expiry);
        var carryInUsed = Math.Min(carryIn, usedBeforeExpiry);
        var carryOut = Math.Max(0, entitlement + adjustments - (used - carryInUsed));

        return new YearStanding(year, entitlement, adjustments, carryIn, carryInUsed, used, usedBeforeExpiry, carryOut);
    }

    /// <summary>
    /// The standing for <paramref name="year"/>, following the carry-over chain from the
    /// first year that counts.
    /// </summary>
    /// <param name="annualDays">The yearly right of the person.</param>
    /// <param name="employmentDate">When they started; nothing counts before it.</param>
    /// <param name="year">The year asked about.</param>
    /// <param name="adjustmentsByYear">Manual corrections, positive or negative, by the year they apply to.</param>
    /// <param name="daysTaken">Every approved annual-leave working day, in any year.</param>
    public static YearStanding Standing(
        int annualDays,
        DateOnly employmentDate,
        int year,
        IReadOnlyDictionary<int, int> adjustmentsByYear,
        IReadOnlyCollection<DateOnly> daysTaken)
    {
        var first = Math.Max(employmentDate.Year, year - MaxCarryYears);
        first = Math.Min(first, year);

        var carry = 0;
        YearStanding? standing = null;

        for (var y = first; y <= year; y++)
        {
            standing = Year(
                y,
                Entitlement(annualDays, employmentDate, y),
                adjustmentsByYear.GetValueOrDefault(y),
                carry,
                daysTaken);

            carry = standing.CarryOut;
        }

        return standing!;
    }

    /// <summary>
    /// Days still available on <paramref name="asOf"/>: this year's right and corrections,
    /// plus whatever carried-over days can still be used, less every day taken in the year
    /// (booked ahead too).
    /// </summary>
    public static int Remaining(YearStanding standing, DateOnly asOf)
    {
        var carryStillValid = asOf < CarryOverExpiry(standing.Year)
            ? standing.CarryIn
            : standing.CarryInUsed;

        return standing.Entitlement + standing.Adjustments + carryStillValid - standing.UsedDays;
    }

    /// <summary>The carried-over days already lost on <paramref name="asOf"/>, for the record.</summary>
    public static int CarryExpired(YearStanding standing, DateOnly asOf) =>
        asOf < CarryOverExpiry(standing.Year) ? 0 : standing.CarryIn - standing.CarryInUsed;
}
