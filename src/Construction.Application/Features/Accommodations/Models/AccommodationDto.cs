using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Accommodations.Models;

public class AccommodationDto
{
    public Guid Id { get; init; }

    public string Address { get; init; } = null!;

    public string? Name { get; init; }

    public AccommodationType Type { get; init; }

    public string? City { get; init; }

    public string? Floor { get; init; }

    public int? Rooms { get; init; }

    public int? Beds { get; init; }

    public decimal? AreaSquareMeters { get; init; }

    public string? LandlordName { get; init; }

    public string? LandlordPhone { get; init; }

    public string? LandlordEmail { get; init; }

    public string? ContractNumber { get; init; }

    public DateOnly? ContractStart { get; init; }

    public DateOnly? ContractEnd { get; init; }

    public decimal? DepositAmount { get; init; }

    public bool UtilitiesIncluded { get; init; }

    public bool IsActive { get; init; }

    public string? Note { get; init; }

    /// <summary>How many people live there today. Filled in after the query, since "today" is not part of the row.</summary>
    public int CurrentOccupants { get; set; }

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
            Name = accommodation.Name,
            Type = accommodation.Type,
            City = accommodation.City,
            Floor = accommodation.Floor,
            Rooms = accommodation.Rooms,
            Beds = accommodation.Beds,
            AreaSquareMeters = accommodation.AreaSquareMeters,
            LandlordName = accommodation.LandlordName,
            LandlordPhone = accommodation.LandlordPhone,
            LandlordEmail = accommodation.LandlordEmail,
            ContractNumber = accommodation.ContractNumber,
            ContractStart = accommodation.ContractStart,
            ContractEnd = accommodation.ContractEnd,
            DepositAmount = accommodation.DepositAmount,
            UtilitiesIncluded = accommodation.UtilitiesIncluded,
            IsActive = accommodation.IsActive,
            CurrentOccupants = 0,
            Note = accommodation.Note,
            CurrentMonthlyAmount = accommodation.Rates
                .Where(r => r.EndDate == null && r.Kind == AccommodationChargeKind.Monthly)
                .Select(r => (decimal?)r.Amount)
                .FirstOrDefault(),
            CurrentProvider = accommodation.Rates
                .Where(r => r.EndDate == null && r.Kind == AccommodationChargeKind.Monthly)
                .Select(r => r.Provider)
                .FirstOrDefault(),
            CreatedAt = accommodation.CreatedAt,
            UpdatedAt = accommodation.UpdatedAt,
        };

    private static readonly Func<Accommodation, AccommodationDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static AccommodationDto ToDto(Accommodation accommodation) => Compiled(accommodation);
}
