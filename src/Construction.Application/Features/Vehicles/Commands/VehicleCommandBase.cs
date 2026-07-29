using Construction.Domain.Enums;
using FluentValidation;

namespace Construction.Application.Features.Vehicles.Commands;

/// <summary>
/// Shared payload for creating and updating a vehicle, so the field rules
/// exist exactly once. Employee assignment is managed through the dedicated
/// assign/unassign endpoints, never through create/update.
/// </summary>
public abstract record VehicleCommandBase
{
    public string Brand { get; init; } = null!;

    public string Model { get; init; } = null!;

    public string RegistrationNumber { get; init; } = null!;

    public string? Vin { get; init; }

    public FuelType FuelType { get; init; }

    public VehicleStatus Status { get; init; } = VehicleStatus.Available;
}

public abstract class VehicleCommandBaseValidator<T> : AbstractValidator<T>
    where T : VehicleCommandBase
{
    protected VehicleCommandBaseValidator()
    {
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(100);

        RuleFor(x => x.RegistrationNumber)
            .NotEmpty().WithMessage("Registration number is required.")
            .MaximumLength(32);

        RuleFor(x => x.Vin)
            .MaximumLength(32);

        RuleFor(x => x.FuelType)
            .IsInEnum().WithMessage("Fuel type is required and must be a valid value.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is not a valid vehicle status.");
    }
}
