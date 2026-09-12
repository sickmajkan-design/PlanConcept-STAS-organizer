using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.ReturnVehicleRentalOut;

/// <summary>
/// Closes an open loan-out — the vehicle came back. A dedicated action,
/// deliberately separate from <c>UpdateVehicleRentalOutCommand</c>: setting
/// <see cref="VehicleRentalOut.EndDate"/> also flips the vehicle's status
/// back to <see cref="VehicleStatus.Available"/>, and a generic "correct a
/// typo" command must never do that as a side effect of an unrelated field
/// edit.
/// </summary>
public record ReturnVehicleRentalOutCommand : IRequest<VehicleRentalOutDto>
{
    public Guid Id { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? EndDate { get; init; }
}

public class ReturnVehicleRentalOutCommandValidator : AbstractValidator<ReturnVehicleRentalOutCommand>
{
    public ReturnVehicleRentalOutCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ReturnVehicleRentalOutCommandHandler
    : IRequestHandler<ReturnVehicleRentalOutCommand, VehicleRentalOutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReturnVehicleRentalOutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleRentalOutDto> Handle(
        ReturnVehicleRentalOutCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not return vehicle rentals.");
        }

        var rental = await _context.VehicleRentalsOut
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleRentalOut), request.Id);

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                if (rental.EndDate is not null)
                {
                    throw new ConflictException("This loan has already been returned.");
                }

                var endDate = request.EndDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

                if (endDate < rental.StartDate)
                {
                    throw new ConflictException("The loan cannot end before it started.");
                }

                var vehicle = await _context.Vehicles
                    .FirstAsync(v => v.Id == rental.VehicleId, token);

                rental.EndDate = endDate;
                vehicle.Status = VehicleStatus.Available;

                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        return await _context.VehicleRentalsOut
            .AsNoTracking()
            .Where(r => r.Id == rental.Id)
            .Select(VehicleRentalOutMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
