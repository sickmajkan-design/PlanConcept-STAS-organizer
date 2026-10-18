using Construction.Application.Common;

namespace Construction.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    /// <summary>
    /// Today on the company's own calendar (<see cref="CompanyTime"/>), not the UTC date, which is yesterday for
    /// the first hours after local midnight. Anything that means "today" asks for this.
    /// </summary>
    DateOnly Today => CompanyTime.LocalDate(UtcNow);
}
