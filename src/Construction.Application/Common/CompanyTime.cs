namespace Construction.Application.Common;

/// <summary>
/// The company's own time zone, and what a moment looks like in it.
/// </summary>
/// <remarks>
/// <para>
/// Every instant in the database is UTC. A <em>day</em>, though — which day a shift belongs to, whether it fell
/// on a Saturday, what "today" is when someone opens the app — is the company's own calendar day, and for a firm
/// in Serbia that is a day that starts two hours (one in winter) before UTC's. Taking the date of a UTC instant
/// puts a shift that begins at 00:30 on Saturday into Friday, and for the first hours after midnight makes
/// "today" yesterday.
/// </para>
/// <para>
/// The zone is one setting (<c>Company:TimeZone</c>, default <see cref="DefaultZoneId"/>) read once at start-up.
/// Nothing else in the application asks the machine's own zone, which is what a server happens to be set to.
/// </para>
/// </remarks>
public static class CompanyTime
{
    public const string DefaultZoneId = "Europe/Belgrade";

    private static TimeZoneInfo _zone = Find(DefaultZoneId) ?? TimeZoneInfo.Utc;

    /// <summary>The zone's id, as PostgreSQL's own <c>timezone()</c> knows it.</summary>
    public static string ZoneId => _zone.Id;

    public static TimeZoneInfo Zone => _zone;

    /// <summary>
    /// Sets the zone. An id the machine does not know falls back to UTC rather than stopping the application, and
    /// <paramref name="problem"/> says so for the caller to log: a wrong day is better than no service.
    /// </summary>
    public static void Configure(string? zoneId, out string? problem)
    {
        problem = null;
        var id = string.IsNullOrWhiteSpace(zoneId) ? DefaultZoneId : zoneId.Trim();
        var found = Find(id);

        if (found is null)
        {
            problem = $"The time zone '{id}' is not known on this machine; UTC is used.";
            found = TimeZoneInfo.Utc;
        }

        _zone = found;
    }

    // Each takes an optional zone: the company's own when it is left out, a named one when a test wants to say which.

    /// <summary>An instant as the company's clock reads it.</summary>
    public static DateTime ToLocal(DateTime utc, TimeZoneInfo? zone = null) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone ?? _zone);

    /// <summary>The company's calendar day an instant falls on.</summary>
    public static DateOnly LocalDate(DateTime utc, TimeZoneInfo? zone = null) => DateOnly.FromDateTime(ToLocal(utc, zone));

    /// <summary>The time of day on the company's clock.</summary>
    public static TimeOnly LocalTimeOfDay(DateTime utc, TimeZoneInfo? zone = null) => TimeOnly.FromDateTime(ToLocal(utc, zone));

    /// <summary>The instant the company's day begins at: its own midnight, as UTC.</summary>
    public static DateTime StartOfDayUtc(DateOnly day, TimeZoneInfo? zone = null) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone ?? _zone);

    /// <summary>
    /// The instants that bound the days <paramref name="from"/> to <paramref name="to"/> inclusive: the first
    /// midnight, and the one after the last day. A time is inside when <c>Start &lt;= t &amp;&amp; t &lt; EndExclusive</c>.
    /// </summary>
    public static (DateTime Start, DateTime EndExclusive) Window(DateOnly from, DateOnly to, TimeZoneInfo? zone = null) =>
        (StartOfDayUtc(from, zone), StartOfDayUtc(to.AddDays(1), zone));

    /// <summary>
    /// An instant in a named zone, as the date and time shown there. In a query this is PostgreSQL's
    /// <c>timezone(zone, instant)</c>, so a day can be taken in the database; written in C# it does the same
    /// conversion. The zone is passed rather than read, so it is a query parameter and the cached query stays
    /// right if the setting ever changes.
    /// </summary>
    public static DateTime InZone(DateTime utc, string zoneId) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById(zoneId));

    private static TimeZoneInfo? Find(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
