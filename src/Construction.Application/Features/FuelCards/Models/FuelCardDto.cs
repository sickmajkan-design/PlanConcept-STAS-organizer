using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.FuelCards.Models;

public class FuelCardDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string VehicleName { get; init; } = null!;

    public string Provider { get; init; } = null!;

    public string CardNumber { get; init; } = null!;

    public DateOnly? IssuedOn { get; init; }

    public string? Note { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>How a <see cref="FuelCard"/> becomes a <see cref="FuelCardDto"/>.</summary>
/// <remarks>See <c>EmployeeMapping</c> for the convention these all follow.</remarks>
public static class FuelCardMapping
{
    public static readonly Expression<Func<FuelCard, FuelCardDto>> Projection = card =>
        new FuelCardDto
        {
            Id = card.Id,
            VehicleId = card.VehicleId,
            VehicleName = card.Vehicle.Brand + " " + card.Vehicle.Model
                + " (" + card.Vehicle.RegistrationNumber + ")",
            Provider = card.Provider,
            CardNumber = card.CardNumber,
            IssuedOn = card.IssuedOn,
            Note = card.Note,
            CreatedAt = card.CreatedAt,
        };

    private static readonly Func<FuelCard, FuelCardDto> Compiled = Projection.Compile();

    public static FuelCardDto ToDto(FuelCard card) => Compiled(card);
}
