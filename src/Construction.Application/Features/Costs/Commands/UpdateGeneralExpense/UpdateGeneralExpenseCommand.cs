using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateGeneralExpense;

/// <summary>Corrects a previously recorded general expense.</summary>
public record UpdateGeneralExpenseCommand : IRequest<GeneralExpenseDto>
{
    public Guid Id { get; init; }

    public GeneralExpenseCategory Category { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? EmployeeId { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class UpdateGeneralExpenseCommandValidator
    : AbstractValidator<UpdateGeneralExpenseCommand>
{
    public UpdateGeneralExpenseCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();

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

public class UpdateGeneralExpenseCommandHandler
    : IRequestHandler<UpdateGeneralExpenseCommand, GeneralExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateGeneralExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GeneralExpenseDto> Handle(
        UpdateGeneralExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanDeleteSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct costs.");
        }

        var expense = await _context.GeneralExpenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneralExpense), request.Id);

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

        expense.Category = request.Category;
        expense.Amount = request.Amount;
        expense.OccurredOn = request.OccurredOn;
        expense.ProjectId = request.ProjectId;
        expense.EmployeeId = request.EmployeeId;
        expense.Supplier = request.Supplier?.Trim();
        expense.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.GeneralExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(GeneralExpenseMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
