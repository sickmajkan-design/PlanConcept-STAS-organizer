using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Commands.UpdateAccommodation;

public record UpdateAccommodationCommand : AccommodationCommandBase, IRequest<AccommodationDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid Id { get; init; }
}

public class UpdateAccommodationCommandValidator
    : AccommodationCommandBaseValidator<UpdateAccommodationCommand>;

public class UpdateAccommodationCommandHandler
    : IRequestHandler<UpdateAccommodationCommand, AccommodationDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateAccommodationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccommodationDto> Handle(
        UpdateAccommodationCommand request,
        CancellationToken cancellationToken)
    {
        var accommodation = await _context.Accommodations
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Accommodation), request.Id);

        accommodation.Address = request.Address.Trim();
        accommodation.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Accommodations
            .AsNoTracking()
            .Where(a => a.Id == accommodation.Id)
            .Select(AccommodationMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
