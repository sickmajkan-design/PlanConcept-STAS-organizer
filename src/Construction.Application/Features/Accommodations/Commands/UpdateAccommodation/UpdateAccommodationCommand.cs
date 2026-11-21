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
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUser;

    public UpdateAccommodationCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUser)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUser = currentUser;
    }

    public async Task<AccommodationDto> Handle(
        UpdateAccommodationCommand request,
        CancellationToken cancellationToken)
    {
        var (own, seesAmounts) = await AccommodationVisibility.ForCallerAsync(
            _context, _currentUser, _dateTimeProvider, cancellationToken);

        var accommodation = await _context.Accommodations
            .Narrow(own)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Accommodation), request.Id);

        await Branches.BranchLookup.EnsureExistsAsync(_context, request.BranchId, cancellationToken);

        var deposit = accommodation.DepositAmount;

        AccommodationFieldMapper.Apply(accommodation, request);

        if (!seesAmounts)
        {
            // The form was filled from a record with the amounts hidden; saving it must not wipe them.
            accommodation.DepositAmount = deposit;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var dto = await _context.Accommodations
            .AsNoTracking()
            .Where(a => a.Id == accommodation.Id)
            .Select(AccommodationMapping.Projection)
            .FirstAsync(cancellationToken);

        await AccommodationOccupancy.FillCurrentOccupantsAsync(
            _context,
            [dto],
            _dateTimeProvider.Today,
            cancellationToken);

        if (!seesAmounts)
        {
            AccommodationVisibility.HideAmounts([dto]);
        }

        return dto;
    }
}
