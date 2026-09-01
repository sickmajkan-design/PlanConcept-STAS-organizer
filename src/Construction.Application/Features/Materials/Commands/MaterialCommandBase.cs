using FluentValidation;

namespace Construction.Application.Features.Materials.Commands;

/// <summary>
/// Shared payload for creating and updating a material, so the field rules
/// exist exactly once. Quantity here is an absolute value; day-to-day stock
/// movements go through the adjust endpoint instead.
/// </summary>
public abstract record MaterialCommandBase
{
    public string Name { get; init; } = null!;

    public string Unit { get; init; } = null!;

    public decimal Quantity { get; init; }

    public string? Warehouse { get; init; }

    /// <summary>Reference price per unit — optional, a planning figure rather than a recorded cost.</summary>
    public decimal? UnitPrice { get; init; }

    public Guid? ProjectId { get; init; }
}

public abstract class MaterialCommandBaseValidator<T> : AbstractValidator<T>
    where T : MaterialCommandBase
{
    protected MaterialCommandBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Material name is required.")
            .MaximumLength(256);

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit of measure is required.")
            .MaximumLength(32);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must not be negative.");

        RuleFor(x => x.Warehouse)
            .MaximumLength(256);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Price must not be negative.")
            .When(x => x.UnitPrice is not null);
    }
}
