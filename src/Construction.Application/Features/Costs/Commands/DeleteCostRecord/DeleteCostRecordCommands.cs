using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.DeleteCostRecord;

// Removing a recorded amount, for the four ledgers. Each narrows the query
// before reading, so a record belonging to something the caller cannot see
// answers 404 rather than 403 — a 403 would confirm that a guessed id is real.

public record DeleteVehicleExpenseCommand(Guid Id) : IRequest;

public class DeleteVehicleExpenseCommandHandler
    : IRequestHandler<DeleteVehicleExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteVehicleExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        DeleteVehicleExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanDeleteSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not remove recorded costs.");
        }

        var expense = await _context.VehicleExpenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleExpense), request.Id);

        _context.VehicleExpenses.Remove(expense);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteEmployeeRateCommand(Guid Id) : IRequest;

public class DeleteEmployeeRateCommandHandler : IRequestHandler<DeleteEmployeeRateCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteEmployeeRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        DeleteEmployeeRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not remove pay rates.");
        }

        var rate = await _context.EmployeeRates
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeRate), request.Id);

        _context.EmployeeRates.Remove(rate);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Removes a stock movement and puts the quantity back the way it was.
/// </summary>
/// <remarks>
/// The reversal is the whole point. Deleting the row alone would leave the
/// running total holding the effect of a movement that no longer exists, and
/// the stock screen would be quietly wrong with nothing to point at.
/// </remarks>
public record DeleteMaterialMovementCommand(Guid Id) : IRequest;

public class DeleteMaterialMovementCommandHandler
    : IRequestHandler<DeleteMaterialMovementCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteMaterialMovementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(
        DeleteMaterialMovementCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanDeleteSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not remove recorded movements.");
        }

        var movement = await _context.MaterialMovements
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(MaterialMovement), request.Id);

        var reversal = -movement.SignedQuantity;
        var materialId = movement.MaterialId;
        var utcNow = _dateTimeProvider.UtcNow;

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                var updated = await _context.Materials
                    .Where(m => m.Id == materialId && m.Quantity + reversal >= 0)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.Quantity, m => m.Quantity + reversal)
                            .SetProperty(m => m.LastUpdated, utcNow)
                            .SetProperty(m => m.UpdatedAt, utcNow),
                        token);

                if (updated == 0)
                {
                    // Undoing a delivery that has since been issued out would
                    // drive the shelf below zero. The right fix is a
                    // correction, not a rewrite of history.
                    throw new ConflictException(
                        "Undoing that movement would put the stock below zero.");
                }

                _context.MaterialMovements.Remove(movement);
                await _context.SaveChangesAsync(token);
            },
            cancellationToken);
    }
}

public record DeleteFinanceEntryCommand(Guid Id) : IRequest;

public class DeleteFinanceEntryCommandHandler : IRequestHandler<DeleteFinanceEntryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteFinanceEntryCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        DeleteFinanceEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not remove pay entries.");
        }

        var entry = await _context.FinanceEntries
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FinanceEntry), request.Id);

        _context.FinanceEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
