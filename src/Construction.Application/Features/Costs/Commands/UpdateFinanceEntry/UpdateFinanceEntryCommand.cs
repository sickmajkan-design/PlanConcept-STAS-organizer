using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateFinanceEntry;

/// <summary>Corrects a previously recorded pay entry.</summary>
public record UpdateFinanceEntryCommand : IRequest<FinanceEntryDto>
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public FinanceEntryKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public Guid? ProjectId { get; init; }

    /// <summary>Required for hourly pay, refused for everything else.</summary>
    public decimal? HoursWorked { get; init; }

    public string? Note { get; init; }
}

public class UpdateFinanceEntryCommandValidator : AbstractValidator<UpdateFinanceEntryCommand>
{
    public UpdateFinanceEntryCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("An amount cannot be negative.");

        RuleFor(x => x.HoursWorked)
            .NotNull().WithMessage("Say how many hours were paid for.")
            .GreaterThanOrEqualTo(0).WithMessage("Hours cannot be negative.")
            .When(x => x.Kind == FinanceEntryKind.WorkerPaymentHourly);

        RuleFor(x => x.HoursWorked)
            .Null().WithMessage("Only hourly pay carries hours.")
            .When(x => x.Kind != FinanceEntryKind.WorkerPaymentHourly);

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("Pay cannot be recorded for the future.")
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"Pay cannot be recorded more than {CostRules.MaxBackdatingDays} days back.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateFinanceEntryCommandHandler
    : IRequestHandler<UpdateFinanceEntryCommand, FinanceEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateFinanceEntryCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<FinanceEntryDto> Handle(
        UpdateFinanceEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct pay entries.");
        }

        var entry = await _context.FinanceEntries
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceEntry), request.Id);

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        entry.EmployeeId = request.EmployeeId;
        entry.Kind = request.Kind;
        entry.Amount = request.Amount;
        entry.OccurredOn = request.OccurredOn;
        entry.ProjectId = request.ProjectId;
        entry.HoursWorked = request.Kind == FinanceEntryKind.WorkerPaymentHourly
            ? request.HoursWorked
            : null;
        entry.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.FinanceEntries
            .AsNoTracking()
            .Where(e => e.Id == entry.Id)
            .Select(FinanceEntryMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
