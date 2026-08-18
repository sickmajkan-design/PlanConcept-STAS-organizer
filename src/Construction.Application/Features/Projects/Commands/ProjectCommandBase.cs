using Construction.Domain.Enums;
using FluentValidation;

namespace Construction.Application.Features.Projects.Commands;

/// <summary>
/// Shared payload for creating and updating a project, so the field rules
/// exist exactly once.
/// </summary>
public abstract record ProjectCommandBase
{
    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string? Client { get; init; }

    public string? Address { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public ProjectStatus Status { get; init; } = ProjectStatus.Planned;

    /// <summary>The total agreed value of the contract, if one has been set.</summary>
    public decimal? ContractValue { get; init; }
}

public abstract class ProjectCommandBaseValidator<T> : AbstractValidator<T>
    where T : ProjectCommandBase
{
    protected ProjectCommandBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(256);

        RuleFor(x => x.Description)
            .MaximumLength(4000);

        RuleFor(x => x.Client)
            .MaximumLength(256);

        RuleFor(x => x.Address)
            .MaximumLength(512);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude is not null);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude is not null);

        RuleFor(x => x)
            .Must(x => x.Latitude is null == x.Longitude is null)
            .WithMessage("Latitude and longitude must be provided together.")
            .OverridePropertyName(nameof(ProjectCommandBase.Latitude));

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("End date must not be before the start date.")
            .When(x => x.StartDate is not null && x.EndDate is not null);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is not a valid project status.");

        RuleFor(x => x.ContractValue)
            .GreaterThanOrEqualTo(0).WithMessage("Contract value cannot be negative.")
            .When(x => x.ContractValue is not null);
    }
}
