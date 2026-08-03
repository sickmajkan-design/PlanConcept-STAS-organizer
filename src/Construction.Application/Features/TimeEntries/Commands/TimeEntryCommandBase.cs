using Construction.Domain.Enums;
using FluentValidation;

namespace Construction.Application.Features.TimeEntries.Commands;

/// <summary>
/// Shared payload for a supervisor recording or correcting a shift by hand,
/// so the field rules exist exactly once.
/// </summary>
/// <remarks>
/// Distinct from clocking in and out, which take no times at all: those come
/// from the server clock. This is the path for the shift someone worked with
/// a flat phone, and for fixing the one they forgot to close.
/// </remarks>
public abstract record TimeEntryCommandBase
{
    public Guid EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    public DateTime StartedAt { get; init; }

    /// <summary>Null records a shift that is still running.</summary>
    public DateTime? EndedAt { get; init; }

    public int BreakMinutes { get; init; }

    public WorkType WorkType { get; init; } = WorkType.Regular;

    public string? Note { get; init; }
}

public abstract class TimeEntryCommandBaseValidator<T> : AbstractValidator<T>
    where T : TimeEntryCommandBase
{
    protected TimeEntryCommandBaseValidator(Construction.Application.Common.Interfaces.IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee is required.");

        RuleFor(x => x.StartedAt)
            .NotEmpty().WithMessage("Start time is required.")
            .Must(start => AsUtc(start) <= dateTimeProvider.UtcNow.AddMinutes(5))
            .WithMessage("A shift cannot start in the future.")
            .Must(start => AsUtc(start) >= dateTimeProvider.UtcNow - TimeEntryRules.MaxBackdating)
            .WithMessage(
                $"A shift cannot be recorded more than " +
                $"{TimeEntryRules.MaxBackdating.TotalDays:0} days back.");

        RuleFor(x => x.EndedAt)
            .Must((command, end) => AsUtc(end!.Value) > AsUtc(command.StartedAt))
            .WithMessage("The shift must end after it starts.")
            .Must((command, end) =>
                AsUtc(end!.Value) - AsUtc(command.StartedAt) <= TimeEntryRules.MaxShiftDuration)
            .WithMessage(
                $"A shift cannot be longer than " +
                $"{TimeEntryRules.MaxShiftDuration.TotalHours:0} hours.")
            .Must(end => AsUtc(end!.Value) <= dateTimeProvider.UtcNow.AddMinutes(5))
            .WithMessage("A shift cannot end in the future.")
            .When(x => x.EndedAt is not null);

        RuleFor(x => x.BreakMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Break must not be negative.");

        // Only checkable once both ends are known; a running shift has no
        // duration to compare a break against yet. Truncated to whole minutes
        // to match what the entry will report — comparing the fractional
        // duration would admit a break that leaves zero paid minutes.
        RuleFor(x => x)
            .Must(x => x.BreakMinutes
                < (int)(AsUtc(x.EndedAt!.Value) - AsUtc(x.StartedAt)).TotalMinutes)
            .WithMessage("The break is as long as the shift, which would leave no time worked.")
            // Named so the 400 response points at a field the client can
            // highlight, instead of an error with no property at all.
            .OverridePropertyName(nameof(TimeEntryCommandBase.BreakMinutes))
            .When(x => x.EndedAt is not null && x.EndedAt > x.StartedAt);

        RuleFor(x => x.WorkType).IsInEnum();

        RuleFor(x => x.Note).MaximumLength(1000);
    }

    private static DateTime AsUtc(DateTime value) => TimeEntryRules.AsUtc(value);
}
