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

        _context.Accommodations.Remove(accommodation);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
