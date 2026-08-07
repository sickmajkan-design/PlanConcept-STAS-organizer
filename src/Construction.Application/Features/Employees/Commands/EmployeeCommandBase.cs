using Construction.Domain.Enums;
using FluentValidation;

namespace Construction.Application.Features.Employees.Commands;

/// <summary>
/// Shared payload for creating and updating an employee, so the field rules
/// exist exactly once.
/// </summary>
public abstract record EmployeeCommandBase
{
    public string EmployeeNumber { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public DateOnly EmploymentDate { get; init; }

    public string Position { get; init; } = null!;

    public EmployeeStatus Status { get; init; } = EmployeeStatus.Active;
}

public abstract class EmployeeCommandBaseValidator<T> : AbstractValidator<T>
    where T : EmployeeCommandBase
{
    protected EmployeeCommandBaseValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("Employee number is required.")
            .MaximumLength(32);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(32);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Address)
            .MaximumLength(512);

        RuleFor(x => x.EmploymentDate)
            .NotEmpty().WithMessage("Employment date is required.");

        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob is null || dob < DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .Must((cmd, dob) => dob is null || dob < cmd.EmploymentDate)
            .WithMessage("Date of birth must be before the employment date.");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Position is required.")
            .MaximumLength(128);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is not a valid employee status.");
    }
}
