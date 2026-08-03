using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using FluentValidation;

namespace Construction.Application.Features.WorkItems.Commands;

/// <summary>Shared payload for raising and editing work, so the field rules
/// exist exactly once.</summary>
public abstract record WorkItemCommandBase
{
    public WorkItemKind Kind { get; init; } = WorkItemKind.Task;

    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    /// <summary>Required for a defect; the database refuses one without it.</summary>
    public Guid? ProjectId { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    public WorkItemPriority Priority { get; init; } = WorkItemPriority.Normal;

    public DateOnly? DueDate { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public abstract class WorkItemCommandBaseValidator<T> : AbstractValidator<T>
    where T : WorkItemCommandBase
{
    /// <summary>
    /// How far ahead a deadline may be set. Two years is longer than any
    /// construction programme this tracks; beyond that it is a typo in a year.
    /// </summary>
    public static readonly TimeSpan MaxLeadTime = TimeSpan.FromDays(730);

    protected WorkItemCommandBaseValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("A title is required.")
            .MaximumLength(256);

        RuleFor(x => x.Description).MaximumLength(4000);

        // Mirrors the database's check constraint, so the message names the
        // field instead of arriving as a constraint violation.
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("A defect has to be raised against a site.")
            .When(x => x.Kind == WorkItemKind.Defect);

        RuleFor(x => x.DueDate)
            .Must(due => due!.Value <= DateOnly
                .FromDateTime(dateTimeProvider.UtcNow)
                .AddDays((int)MaxLeadTime.TotalDays))
            .WithMessage(
                $"A deadline more than {MaxLeadTime.TotalDays / 365:0} years out is probably a typo.")
            .When(x => x.DueDate is not null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude is not null);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude is not null);

        RuleFor(x => x)
            .Must(x => x.Latitude is null == x.Longitude is null)
            .WithMessage("Latitude and longitude must be supplied together.")
            .OverridePropertyName(nameof(WorkItemCommandBase.Longitude));
    }
}
