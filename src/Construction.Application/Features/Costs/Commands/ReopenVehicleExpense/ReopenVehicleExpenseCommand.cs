using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.ReopenVehicleExpense;

/// <summary>
/// Takes back a decision on a vehicle cost: approved or sent back becomes
/// pending again, so it can be looked at afresh.
/// </summary>
/// <remarks>
/// The way to undo an approval made by mistake. It is not a review, so it
/// needs no reason, but it follows the same rules about who may act: reviewers
/// only, and never on their own entry (a Super Admin excepted). The change is
/// in the audit trail like any other.
/// </remarks>
public record ReopenVehicleExpenseCommand : IRequest<VehicleExpenseDto>
{
    public Guid Id { get; init; }
}

public class ReopenVehicleExpenseCommandValidator : AbstractValidator<ReopenVehicleExpenseCommand>
{
    public ReopenVehicleExpenseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ReopenVehicleExpenseCommandHandler
    : IRequestHandler<ReopenVehicleExpenseCommand, VehicleExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReopenVehicleExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<VehicleExpenseDto> Handle(
        ReopenVehicleExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanReviewSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not review vehicle costs.");
        }

        var expense = await _context.VehicleExpenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleExpense), request.Id);

        if (expense.RecordedByUserId is { } recordedByUserId
            && recordedByUserId == _currentUserService.UserId
            && _currentUserService.Role != UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("You cannot review a cost you recorded yourself.");
        }

        if (expense.Status == VehicleExpenseStatus.Pending)
        {
            throw new ConflictException("This cost is already waiting for review.");
        }

        expense.Status = VehicleExpenseStatus.Pending;
        expense.ReviewNote = null;
        expense.ReviewedByUserId = null;
        expense.ReviewedAt = null;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This cost was changed by someone else just now. Reload it and try again.");
        }

        return await _context.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(VehicleExpenseMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
