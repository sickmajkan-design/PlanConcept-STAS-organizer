using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelCards.Commands;

/// <summary>
/// Retires a fuel card (soft delete), so its number can be reused by a
/// replacement card later without a hard delete losing the history of what
/// was imported against it.
/// </summary>
public record DeleteFuelCardCommand(Guid Id) : IRequest;

public class DeleteFuelCardCommandHandler : IRequestHandler<DeleteFuelCardCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteFuelCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteFuelCardCommand request, CancellationToken cancellationToken)
    {
        if (!CostRules.CanDeleteSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not remove fuel cards.");
        }

        var card = await _context.FuelCards
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FuelCard), request.Id);

        card.IsDeleted = true;
        card.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
