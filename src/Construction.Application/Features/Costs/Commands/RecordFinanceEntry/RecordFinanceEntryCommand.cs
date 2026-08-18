using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.RecordFinanceEntry;

/// <summary>Records what the office decided to pay an employee for a stretch of work.</summary>
public record RecordFinanceEntryCommand : IRequest<FinanceEntryDto>
{
    public Guid EmployeeId { get; init; }

    public FinanceEntryKind Kind { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? OccurredOn { get; init; }

    /// <summary>The site the pay is charged against, when there is one.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Required for hourly pay, refused for everything else.</summary>
    public decimal? HoursWorked { get; init; }

    public string? Note { get; init; }
}

public class RecordFinanceEntryCommandValidator : AbstractValidator<RecordFinanceEntryCommand>
{
    public RecordFinanceEntryCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("An amount cannot be negative.");

        // Mirrors the database's check constraint, so the answer is a
        // sentence on the right field rather than a constraint violation.
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
                $"Pay cannot be recorded more than {CostRules.MaxBackdatingDays} days back.")
            .When(x => x.OccurredOn is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordFinanceEntryCommandHandler
    : IRequestHandler<RecordFinanceEntryCommand, FinanceEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordFinanceEntryCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<FinanceEntryDto> Handle(
        RecordFinanceEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record pay.");
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var entry = new FinanceEntry
        {
            EmployeeId = request.EmployeeId,
            Kind = request.Kind,
            Amount = request.Amount,
            OccurredOn = request.OccurredOn
                ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            ProjectId = request.ProjectId,
            // Belt and braces with the validator and the check constraint: a
            // future caller that skips validation still cannot put hours on a
            // fixed or daily payment.
            HoursWorked = request.Kind == FinanceEntryKind.WorkerPaymentHourly
                ? request.HoursWorked
                : null,
            Note = request.Note?.Trim(),
            RecordedByUserId = _currentUserService.UserId
        };

        _context.FinanceEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.FinanceEntries
            .AsNoTracking()
            .Where(e => e.Id == entry.Id)
            .Select(FinanceEntryMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
