using FluentValidation;

namespace Construction.Application.Features.Customers.Commands;

/// <summary>
/// Shared payload for creating and updating a customer, so the field rules
/// exist exactly once.
/// </summary>
public abstract record CustomerCommandBase
{
    public string Name { get; init; } = null!;

    public string? ContactPerson { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Note { get; init; }
}

public abstract class CustomerCommandBaseValidator<T> : AbstractValidator<T>
    where T : CustomerCommandBase
{
    protected CustomerCommandBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(256);

        RuleFor(x => x.ContactPerson)
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .MaximumLength(64);

        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress().WithMessage("Not a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Note)
            .MaximumLength(2000);
    }
}
