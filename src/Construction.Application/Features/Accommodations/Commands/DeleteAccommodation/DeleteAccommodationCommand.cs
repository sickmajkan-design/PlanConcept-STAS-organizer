using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Commands.DeleteAccommodation;

/// <summary>Soft-deletes an accommodation. Its rate history is kept for audit/history and disappears from queries via the soft-delete filter.</summary>
public record DeleteAccommodationCommand(Guid Id) : IRequest;

public class DeleteAccommodationCommandHandler : IRequestHandler<DeleteAccommodationCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteAccommodationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteAccommodationCommand request, CancellationToken cancellationToken)
    {
        var accommodation = await _context.Accommodations
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Accommodation), request.Id);

        // Stays are not occupancy once the place is gone, but the database's
        // "one place at a time" rule would still count them and block the same
        // person from being housed anywhere else. The stays are not kept for
        // history; the rate history and the deletion itself are.
        await _context.AccommodationStays
            .Where(s => s.AccommodationId == request.Id)
            .ExecuteDeleteAsync(cancellationToken);

        _context.Accommodations.Remove(accommodation);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
