using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateToolExpense;

/// <summary>Corrects a previously recorded tool cost.</summary>
public record UpdateToolExpenseCommand : IRequest<ToolExpenseDto>
{
    public Guid Id { get; init; }

    public Guid ToolId { get; init; }

    public ToolExpenseKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class UpdateToolExpenseCommandValidator : AbstractValidator<UpdateToolExpenseCommand>
{
    public UpdateToolExpenseCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ToolId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("An amount cannot be negative.");

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("A cost cannot be incurred in the future.")
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A cost cannot be recorded more than {CostRules.MaxBackdatingDays} days back.");

        RuleFor(x => x.Supplier).MaximumLength(200);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateToolExpenseCommandHandler
    : IRequestHandler<UpdateToolExpenseCommand, ToolExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateToolExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ToolExpenseDto> Handle(
        UpdateToolExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanDeleteSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct tool costs.");
        }

        var expense = await _context.ToolExpenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ToolExpense), request.Id);

        if (!await _context.Tools.AnyAsync(t => t.Id == request.ToolId, cancellationToken))
        {
            throw new NotFoundException(nameof(Tool), request.ToolId);
        }

        expense.ToolId = request.ToolId;
        expense.Kind = request.Kind;
        expense.Amount = request.Amount;
        expense.OccurredOn = request.OccurredOn;
        expense.Supplier = request.Supplier?.Trim();
        expense.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.ToolExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(ToolExpenseMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
