using Construction.Domain.Enums;
using FluentValidation;

namespace Construction.Application.Features.Tools.Commands;

/// <summary>
/// Shared payload for creating and updating a tool, so the field rules exist
/// exactly once. Assignments are managed through the dedicated
/// assign/unassign endpoints, never through create/update.
/// </summary>
public abstract record ToolCommandBase
{
    public string Name { get; init; } = null!;

    public string? Category { get; init; }

    public string? SerialNumber { get; init; }

    public string? QrCode { get; init; }

    public ToolStatus Status { get; init; } = ToolStatus.Available;
}

public abstract class ToolCommandBaseValidator<T> : AbstractValidator<T>
    where T : ToolCommandBase
{
    protected ToolCommandBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tool name is required.")
            .MaximumLength(256);

        RuleFor(x => x.Category)
            .MaximumLength(128);

        RuleFor(x => x.SerialNumber)
            .MaximumLength(128);

        RuleFor(x => x.QrCode)
            .MaximumLength(256);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is not a valid tool status.");
    }
}
