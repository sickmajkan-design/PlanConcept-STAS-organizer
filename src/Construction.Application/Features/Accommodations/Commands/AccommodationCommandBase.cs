using Construction.Domain.Enums;
using FluentValidation;

namespace Construction.Application.Features.Accommodations.Commands;

/// <summary>
/// Shared payload for creating and updating an accommodation, so the field
/// rules exist exactly once.
/// </summary>
public abstract record AccommodationCommandBase
{
    public string Address { get; init; } = null!;

    public string? Name { get; init; }

    public AccommodationType Type { get; init; } = AccommodationType.Apartment;

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

    public bool IsActive { get; init; } = true;

    public string? Note { get; init; }
}

public abstract class AccommodationCommandBaseValidator<T> : AbstractValidator<T>
    where T : AccommodationCommandBase
{
    protected AccommodationCommandBaseValidator()
    {
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("An address is required.")
            .MaximumLength(512);

        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(120);
        RuleFor(x => x.Floor).MaximumLength(40);

        RuleFor(x => x.Rooms)
            .GreaterThan(0).WithMessage("Rooms must be at least 1.")
            .LessThanOrEqualTo(100)
            .When(x => x.Rooms is not null);

        RuleFor(x => x.Beds)
            .GreaterThan(0).WithMessage("Beds must be at least 1.")
            .LessThanOrEqualTo(500)
            .When(x => x.Beds is not null);

        RuleFor(x => x.AreaSquareMeters)
            .GreaterThan(0).WithMessage("The area must be more than zero.")
            .LessThanOrEqualTo(100_000)
            .When(x => x.AreaSquareMeters is not null);

        RuleFor(x => x.LandlordName).MaximumLength(200);
        RuleFor(x => x.LandlordPhone).MaximumLength(60);

        RuleFor(x => x.LandlordEmail)
            .MaximumLength(200)
            .EmailAddress().WithMessage("That is not an email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.LandlordEmail));

        RuleFor(x => x.ContractNumber).MaximumLength(100);

        RuleFor(x => x.ContractEnd)
            .GreaterThanOrEqualTo(x => x.ContractStart!.Value)
            .WithMessage("The contract cannot end before it starts.")
            .When(x => x.ContractStart is not null && x.ContractEnd is not null);

        RuleFor(x => x.DepositAmount)
            .GreaterThanOrEqualTo(0).WithMessage("A deposit cannot be negative.")
            .When(x => x.DepositAmount is not null);

        RuleFor(x => x.Note)
            .MaximumLength(2000);
    }
}

/// <summary>Copies the shared fields onto the entity, trimming the text ones.</summary>
public static class AccommodationFieldMapper
{
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void Apply(Construction.Domain.Entities.Accommodation target, AccommodationCommandBase source)
    {
        target.Address = source.Address.Trim();
        target.Name = Clean(source.Name);
        target.Type = source.Type;
        target.City = Clean(source.City);
        target.Floor = Clean(source.Floor);
        target.Rooms = source.Rooms;
        target.Beds = source.Beds;
        target.AreaSquareMeters = source.AreaSquareMeters;
        target.LandlordName = Clean(source.LandlordName);
        target.LandlordPhone = Clean(source.LandlordPhone);
        target.LandlordEmail = Clean(source.LandlordEmail);
        target.ContractNumber = Clean(source.ContractNumber);
        target.ContractStart = source.ContractStart;
        target.ContractEnd = source.ContractEnd;
        target.DepositAmount = source.DepositAmount;
        target.UtilitiesIncluded = source.UtilitiesIncluded;
        target.IsActive = source.IsActive;
        target.Note = Clean(source.Note);
    }
}
