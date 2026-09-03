using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.RecordGeneralExpense;

/// <summary>Records a cost that isn't a vehicle's, a tool's or a material's — housing, bookkeeping, damage, and the like.</summary>
public record RecordGeneralExpenseCommand : IRequest<GeneralExpenseDto>
{
    public GeneralExpenseCategory Category { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? OccurredOn { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? EmployeeId { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class RecordGeneralExpenseCommandValidator
    : AbstractValidator<RecordGeneralExpenseCommand>
{
    public RecordGeneralExpenseCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("An amount cannot be negative.");

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("A cost cannot be incurred in the future.")
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A cost cannot be recorded more than {CostRules.MaxBackdatingDays} days back.")
            .When(x => x.OccurredOn is not null);

        RuleFor(x => x.Supplier).MaximumLength(200);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordGeneralExpenseCommandHandler
    : IRequestHandler<RecordGeneralExpenseCommand, GeneralExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordGeneralExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<GeneralExpenseDto> Handle(
        RecordGeneralExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record costs.");
        }

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        if (request.EmployeeId is { } employeeId
            && !await _context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        var expense = new GeneralExpense
        {
            Category = request.Category,
            Amount = request.Amount,
            OccurredOn = request.OccurredOn
                ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            ProjectId = request.ProjectId,
            EmployeeId = request.EmployeeId,
            Supplier = request.Supplier?.Trim(),
            Note = request.Note?.Trim(),
            RecordedByUserId = _currentUserService.UserId
        };

        _context.GeneralExpenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.GeneralExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(GeneralExpenseMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
