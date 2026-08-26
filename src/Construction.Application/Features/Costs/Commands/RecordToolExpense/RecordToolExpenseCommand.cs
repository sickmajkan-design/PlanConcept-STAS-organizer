using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.RecordToolExpense;

/// <summary>Records a repair, a service, or any other cost of a tool.</summary>
public record RecordToolExpenseCommand : IRequest<ToolExpenseDto>
{
    public Guid ToolId { get; init; }

    public ToolExpenseKind Kind { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? OccurredOn { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class RecordToolExpenseCommandValidator : AbstractValidator<RecordToolExpenseCommand>
{
    public RecordToolExpenseCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.ToolId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();

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

public class RecordToolExpenseCommandHandler
    : IRequestHandler<RecordToolExpenseCommand, ToolExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordToolExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToolExpenseDto> Handle(
        RecordToolExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record tool costs.");
        }

        if (!await _context.Tools.AnyAsync(t => t.Id == request.ToolId, cancellationToken))
        {
            throw new NotFoundException(nameof(Tool), request.ToolId);
        }

        var expense = new ToolExpense
        {
            ToolId = request.ToolId,
            Kind = request.Kind,
            Amount = request.Amount,
            OccurredOn = request.OccurredOn
                ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            Supplier = request.Supplier?.Trim(),
            Note = request.Note?.Trim(),
            RecordedByUserId = _currentUserService.UserId
        };

        _context.ToolExpenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.ToolExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(ToolExpenseMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
