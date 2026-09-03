using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Commands.CreateAccommodation;

public record CreateAccommodationCommand : AccommodationCommandBase, IRequest<AccommodationDto>;

public class CreateAccommodationCommandValidator
    : AccommodationCommandBaseValidator<CreateAccommodationCommand>;

public class CreateAccommodationCommandHandler
    : IRequestHandler<CreateAccommodationCommand, AccommodationDto>
{
    private readonly IApplicationDbContext _context;

    public CreateAccommodationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccommodationDto> Handle(
        CreateAccommodationCommand request,
        CancellationToken cancellationToken)
    {
        var accommodation = new Accommodation
        {
            Address = request.Address.Trim(),
            Note = request.Note?.Trim(),
        };

        _context.Accommodations.Add(accommodation);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Accommodations
            .AsNoTracking()
            .Where(a => a.Id == accommodation.Id)
            .Select(AccommodationMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
