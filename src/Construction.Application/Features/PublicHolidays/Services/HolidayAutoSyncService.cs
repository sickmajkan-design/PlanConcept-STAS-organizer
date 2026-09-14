using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.PublicHolidays.Services;

public interface IHolidayAutoSyncService
{
    /// <summary>
    /// Makes sure a country's holiday calendar has something in it for the
    /// years that matter right now, fetching from the internet if it does
    /// not. Never throws — a country the source does not recognise, or a
    /// moment the source is unreachable, is not a reason to fail whatever
    /// the caller was actually doing (saving a project).
    /// </summary>
    Task SyncIfMissingAsync(string? countryCode, CancellationToken cancellationToken);
}

/// <summary>
/// The automatic half of the holiday calendar: a project set to a country
/// with nothing on file for it gets that country's calendar without anyone
/// visiting the Public Holidays page and running the sync by hand. The
/// manual "search country + year, review, import" flow
/// (<c>PreviewHolidaySyncQuery</c>/<c>ImportPublicHolidaysCommand</c>) still
/// exists for a year this does not cover, or a country the source gets
/// wrong.
/// </summary>
public class HolidayAutoSyncService : IHolidayAutoSyncService
{
    private readonly IApplicationDbContext _context;
    private readonly IPublicHolidaySource _source;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<HolidayAutoSyncService> _logger;

    public HolidayAutoSyncService(
        IApplicationDbContext context,
        IPublicHolidaySource source,
        IDateTimeProvider dateTimeProvider,
        ILogger<HolidayAutoSyncService> logger)
    {
        _context = context;
        _source = source;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SyncIfMissingAsync(string? countryCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return;
        }

        var code = countryCode.Trim().ToUpperInvariant();
        var thisYear = _dateTimeProvider.UtcNow.Year;

        // This year and next: enough for a holiday rate to apply correctly
        // to a shift worked any time soon, without fetching years nobody
        // has a project running in yet.
        foreach (var year in new[] { thisYear, thisYear + 1 })
        {
            await SyncYearIfMissingAsync(code, year, cancellationToken);
        }
    }

    private async Task SyncYearIfMissingAsync(string code, int year, CancellationToken cancellationToken)
    {
        var hasAny = await _context.PublicHolidays
            .AnyAsync(h => h.CountryCode == code && h.Date.Year == year, cancellationToken);

        if (hasAny)
        {
            return;
        }

        IReadOnlyList<ExternalHoliday> external;

        try
        {
            external = await _source.GetHolidaysAsync(code, year, cancellationToken);
        }
        catch (ExternalServiceException ex)
        {
            // A code the source doesn't recognise, or the source being down
            // right now — either way, someone can still add the calendar by
            // hand, or the next project save for this country tries again.
            _logger.LogInformation(
                ex, "Could not auto-sync public holidays for {CountryCode} {Year}.", code, year);
            return;
        }

        if (external.Count == 0)
        {
            return;
        }

        var rows = external
            .Select(holiday => new PublicHoliday
            {
                Date = holiday.Date,
                Name = string.IsNullOrWhiteSpace(holiday.LocalName) ? holiday.Name : holiday.LocalName,
                CountryCode = code,
            })
            .ToList();

        _context.PublicHolidays.AddRange(rows);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two projects in a brand-new country saved close together can
            // both decide the calendar is empty and both try to fill it —
            // the unique (country, date) index is the real backstop for
            // that race, and losing it here just means the other request
            // already did this work.
            _logger.LogInformation(
                ex, "Public holidays for {CountryCode} {Year} were synced concurrently.", code, year);
        }
    }
}
