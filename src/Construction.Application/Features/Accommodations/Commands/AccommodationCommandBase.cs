using FluentValidation;

namespace Construction.Application.Features.Accommodations.Commands;

/// <summary>
/// Shared payload for creating and updating an accommodation, so the field
/// rules exist exactly once.
/// </summary>
public abstract record AccommodationCommandBase
{
    public string Address { get; init; } = null!;

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

        RuleFor(x => x.Note)
            .MaximumLength(2000);
    }
}
