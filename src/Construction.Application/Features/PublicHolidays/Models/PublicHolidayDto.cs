using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.PublicHolidays.Models;

public class PublicHolidayDto
{
    public Guid Id { get; init; }

    public DateOnly Date { get; init; }

    public string Name { get; init; } = null!;
}

public static class PublicHolidayMapping
{
    public static readonly Expression<Func<PublicHoliday, PublicHolidayDto>> Projection =
        holiday => new PublicHolidayDto
        {
            Id = holiday.Id,
            Date = holiday.Date,
            Name = holiday.Name,
        };

    private static readonly Func<PublicHoliday, PublicHolidayDto> Compiled = Projection.Compile();

    public static PublicHolidayDto ToDto(PublicHoliday holiday) => Compiled(holiday);
}
