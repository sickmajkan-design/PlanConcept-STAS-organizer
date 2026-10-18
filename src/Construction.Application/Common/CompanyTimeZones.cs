namespace Construction.Application.Common;

/// <summary>
/// The time zones an organisation can choose between: IANA ids, the names PostgreSQL and every platform know, and not
/// the Windows ones a developer machine would offer. Each is an id both .NET and the database understand.
/// </summary>
public static class CompanyTimeZones
{
    public static readonly IReadOnlyList<string> All =
    [
        "UTC",
        // Europe
        "Europe/Belgrade", "Europe/Sarajevo", "Europe/Zagreb", "Europe/Ljubljana", "Europe/Skopje", "Europe/Podgorica",
        "Europe/Vienna", "Europe/Berlin", "Europe/Zurich", "Europe/Prague", "Europe/Bratislava", "Europe/Budapest",
        "Europe/Warsaw", "Europe/Rome", "Europe/Madrid", "Europe/Paris", "Europe/Brussels", "Europe/Amsterdam",
        "Europe/Luxembourg", "Europe/Copenhagen", "Europe/Stockholm", "Europe/Oslo", "Europe/Helsinki", "Europe/Tallinn",
        "Europe/Riga", "Europe/Vilnius", "Europe/Bucharest", "Europe/Sofia", "Europe/Athens", "Europe/Istanbul",
        "Europe/Kyiv", "Europe/Moscow", "Europe/London", "Europe/Dublin", "Europe/Lisbon", "Atlantic/Reykjavik",
        // Middle East and Africa
        "Asia/Dubai", "Asia/Riyadh", "Asia/Qatar", "Asia/Kuwait", "Asia/Baghdad", "Asia/Tehran", "Asia/Jerusalem",
        "Africa/Cairo", "Africa/Johannesburg", "Africa/Lagos", "Africa/Nairobi", "Africa/Casablanca", "Africa/Algiers",
        // Asia and Pacific
        "Asia/Karachi", "Asia/Kolkata", "Asia/Dhaka", "Asia/Bangkok", "Asia/Singapore", "Asia/Hong_Kong", "Asia/Shanghai",
        "Asia/Tokyo", "Asia/Seoul", "Australia/Perth", "Australia/Sydney", "Pacific/Auckland",
        // Americas
        "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles", "America/Toronto",
        "America/Mexico_City", "America/Bogota", "America/Lima", "America/Sao_Paulo", "America/Argentina/Buenos_Aires",
        "America/Santiago",
    ];

    /// <summary>Whether the id is one an organisation may choose and this machine can work with.</summary>
    public static bool IsSupported(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !All.Contains(id.Trim(), StringComparer.Ordinal))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
