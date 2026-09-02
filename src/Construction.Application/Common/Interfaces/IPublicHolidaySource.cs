namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Where the holiday calendar's "sync from the internet" button gets its
/// data. A port rather than a direct dependency on the concrete client, the
/// same shape as <c>IEmailSender</c>/<c>IPushSender</c>, so the Infrastructure
/// implementation (today: the Nager.Date API) can change without the
/// Application layer knowing.
/// </summary>
public interface IPublicHolidaySource
{
    /// <summary>
    /// A country's public holidays for one calendar year. Throws
    /// <see cref="Construction.Application.Common.Exceptions.ExternalServiceException"/>
    /// when the source cannot answer — an unreachable service, a country code
    /// or year it does not recognise.
    /// </summary>
    Task<IReadOnlyList<ExternalHoliday>> GetHolidaysAsync(
        string countryCode,
        int year,
        CancellationToken cancellationToken);
}

/// <summary>One holiday as reported by an external source, before it becomes a row on our calendar.</summary>
public record ExternalHoliday(DateOnly Date, string Name, string LocalName);
