using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.PublicHolidays.Models;

public class PublicHolidayDto
{
    public Guid Id { get; init; }

    public DateOnly Date { get; init; }

    public string Name { get; init; } = null!;

    /// <summary>ISO 3166-1 alpha-2, e.g. "BA".</summary>
    public string CountryCode { get; init; } = null!;
}

public static class PublicHolidayMapping
{
    public static readonly Expression<Func<PublicHoliday, PublicHolidayDto>> Projection =
        holiday => new PublicHolidayDto
        {
            Id = holiday.Id,
            Date = holiday.Date,
            Name = holiday.Name,
            CountryCode = holiday.CountryCode,
        };

    private static readonly Func<PublicHoliday, PublicHolidayDto> Compiled = Projection.Compile();

    public static PublicHolidayDto ToDto(PublicHoliday holiday) => Compiled(holiday);
}

/// <summary>
/// One holiday fetched from the internet for a chosen country/year, offered
/// up for review before anything is written to the calendar.
/// </summary>
public class PublicHolidayCandidateDto
{
    public DateOnly Date { get; init; }

    public string Name { get; init; } = null!;

    /// <summary>Already on the calendar — importing it again would be refused as a conflict.</summary>
    public bool AlreadyOnCalendar { get; init; }
}
