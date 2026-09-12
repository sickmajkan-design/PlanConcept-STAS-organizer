using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateVehicleRentalOut;

/// <summary>
/// Corrects a data-entry mistake on an existing loan-out — who it was to,
/// the rate, when it started, the note.
/// </summary>
/// <remarks>
/// Deliberately narrower than <c>RecordVehicleRentalOutCommand</c> and
/// separate from <c>ReturnVehicleRentalOutCommand</c>: it never touches
/// <see cref="VehicleRentalOut.EndDate"/> or the vehicle's status. Returning
/// the vehicle is its own explicit action.
/// </remarks>
public record UpdateVehicleRentalOutCommand : IRequest<VehicleRentalOutDto>
{
    public Guid Id { get; init; }

    public Guid? CustomerId { get; init; }

    public string RenterName { get; init; } = null!;

    public decimal DailyRate { get; init; }

    public DateOnly StartDate { get; init; }

    public string? Note { get; init; }
}

public class UpdateVehicleRentalOutCommandValidator : AbstractValidator<UpdateVehicleRentalOutCommand>
{
    public UpdateVehicleRentalOutCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.RenterName)
            .NotEmpty().WithMessage("Say who has the vehicle.")
            .MaximumLength(200);

        RuleFor(x => x.DailyRate)
            .GreaterThan(0).WithMessage("A day out has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo rather than a daily rate.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateVehicleRentalOutCommandHandler
    : IRequestHandler<UpdateVehicleRentalOutCommand, VehicleRentalOutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateVehicleRentalOutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<VehicleRentalOutDto> Handle(
        UpdateVehicleRentalOutCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct vehicle rentals.");
        }

        var rental = await _context.VehicleRentalsOut
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleRentalOut), request.Id);

        if (rental.EndDate is not null && request.StartDate > rental.EndDate)
        {
            throw new ConflictException("The loan cannot start after it ended.");
        }

        if (request.CustomerId is { } customerId
            && !await _context.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), customerId);
        }

        rental.CustomerId = request.CustomerId;
        rental.RenterName = request.RenterName.Trim();
        rental.DailyRate = request.DailyRate;
        rental.StartDate = request.StartDate;
        rental.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.VehicleRentalsOut
            .AsNoTracking()
            .Where(r => r.Id == rental.Id)
            .Select(VehicleRentalOutMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
