using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Accommodations.Models;

public class AccommodationDto
{
    public Guid Id { get; init; }

    public string Address { get; init; } = null!;

    public string? Note { get; init; }

    /// <summary>Set when a rate is currently in force (EndDate null). Null for one with no rate on file.</summary>
    public decimal? CurrentMonthlyAmount { get; init; }

    public string? CurrentProvider { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How an <see cref="Accommodation"/> becomes an <see cref="AccommodationDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class AccommodationMapping
{
    public static readonly Expression<Func<Accommodation, AccommodationDto>> Projection = accommodation =>
        new AccommodationDto
        {
            Id = accommodation.Id,
            Address = accommodation.Address,
            Note = accommodation.Note,
            CurrentMonthlyAmount = accommodation.Rates
                .Where(r => r.EndDate == null)
                .Select(r => (decimal?)r.MonthlyAmount)
                .FirstOrDefault(),
            CurrentProvider = accommodation.Rates
                .Where(r => r.EndDate == null)
                .Select(r => r.Provider)
                .FirstOrDefault(),
            CreatedAt = accommodation.CreatedAt,
            UpdatedAt = accommodation.UpdatedAt,
        };

    private static readonly Func<Accommodation, AccommodationDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static AccommodationDto ToDto(Accommodation accommodation) => Compiled(accommodation);
}
