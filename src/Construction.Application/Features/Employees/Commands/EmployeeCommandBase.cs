using Construction.Application.Common;
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

    /// <summary>Direct employee or subcontractor.</summary>
    public EmployeeType Type { get; init; } = EmployeeType.Employee;

    /// <summary>The contractor's firm. Required for a subcontractor, ignored for an employee.</summary>
    public string? ContractorCompany { get; init; }

    /// <summary>When the contractor's engagement ends; null means until further notice. Ignored for an employee.</summary>
    public DateOnly? EngagementEndDate { get; init; }
}

/// <summary>What a subcontractor may and may not be, in one place so every path agrees.</summary>
public static class ContractorRules
{
    /// <summary>A contractor is always a worker in the hierarchy: no higher login role, no higher rank.</summary>
    public static void EnsureWorker(UserRole? role, OrganizationRank? rank)
    {
        if (role is { } r && r != UserRole.Worker)
        {
            throw new Common.Exceptions.ConflictException("A subcontractor can only have the Worker role.");
        }

        if (rank is { } k && k != OrganizationRank.Worker)
        {
            throw new Common.Exceptions.ConflictException("A subcontractor can only be a worker on the org chart.");
        }
    }
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
            .Must(dob => dob is null || dob < CompanyTime.LocalDate(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.")
            .Must((cmd, dob) => dob is null || dob < cmd.EmploymentDate)
            .WithMessage("Date of birth must be before the employment date.");

        RuleFor(x => x.Position)
            .NotEmpty().WithMessage("Position is required.")
            .MaximumLength(128);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is not a valid employee status.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type is not a valid employee type.");

        When(x => x.Type == EmployeeType.Subcontractor, () =>
        {
            // Required on creation (see CreateEmployeeCommandValidator). On an update an absent firm means an
            // older client, such as an installed phone app, that does not know the field: see the handler.
            RuleFor(x => x.ContractorCompany).MaximumLength(200);

            RuleFor(x => x.Phone).NotEmpty().WithMessage("Phone is required for a subcontractor.");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required for a subcontractor.");

            RuleFor(x => x.EngagementEndDate)
                .Must((cmd, end) => end is null || end >= cmd.EmploymentDate)
                .WithMessage("The engagement cannot end before it starts.");
        });
    }
}
