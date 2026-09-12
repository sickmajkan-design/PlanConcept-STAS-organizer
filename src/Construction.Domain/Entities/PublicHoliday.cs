using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A day priced like a holiday, wherever an <see cref="EmployeeRate"/> sets a
/// <see cref="EmployeeRate.HolidayHourlyRate"/> — for shifts on a
/// <see cref="Project"/> whose <see cref="Project.CountryCode"/> matches this
/// holiday's own.
/// </summary>
/// <remarks>
/// One row per (country, date) — nothing recurs automatically from year to
/// year, and a fixed public holiday is entered once per country per year,
/// either by hand or via the "sync from the internet" flow
/// (<c>PreviewHolidaySyncQuery</c>/<c>ImportPublicHolidaysCommand</c>). A
/// moving holiday (tied to a lunar or religious calendar) cannot be derived
/// from a rule anyway, so this stays a plain calendar of dates rather than a
/// recurrence engine.
///
/// The country dimension exists because this company runs sites in more than
/// one country at once: a Croatian public holiday must not give a German
/// site's shift a holiday rate, and vice versa. A hand-added company day off
/// (the office deciding to close for something that is not an official
/// holiday) is still just a row here, scoped to whichever country's crew it
/// applies to.
/// </remarks>
public class PublicHoliday : BaseEntity
{
    public DateOnly Date { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>ISO 3166-1 alpha-2, e.g. "BA", "HR", "DE".</summary>
    public string CountryCode { get; set; } = null!;
}
