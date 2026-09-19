using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Queries.GetAccommodationById;

public record GetAccommodationByIdQuery(Guid Id) : IRequest<AccommodationDto>;

public class GetAccommodationByIdQueryHandler : IRequestHandler<GetAccommodationByIdQuery, AccommodationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccommodationByIdQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AccommodationDto> Handle(
        GetAccommodationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var accommodation = await _context.Accommodations
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(AccommodationMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (accommodation is null)
        {
            throw new NotFoundException(nameof(Accommodation), request.Id);
        }

        await AccommodationOccupancy.FillCurrentOccupantsAsync(
            _context,
            [accommodation],
            DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            cancellationToken);

        return accommodation;
    }
}
