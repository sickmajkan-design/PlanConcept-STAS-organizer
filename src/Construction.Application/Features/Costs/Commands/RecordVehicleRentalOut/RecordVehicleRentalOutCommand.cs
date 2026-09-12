using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.RecordVehicleRentalOut;

/// <summary>Records the vehicle going out to another company — the revenue direction, opposite <see cref="VehicleRentalRate"/>.</summary>
/// <remarks>
/// Each loan-out is its own row, not a dated chain: a company borrowing the
/// same excavator twice in one year is two rows, not one rate replacing
/// another. Requires the vehicle to be <see cref="VehicleStatus.Available"/>
/// and flips it to <see cref="VehicleStatus.RentedOut"/> — equipment already
/// out with someone else, or assigned to the crew, cannot go out again.
/// </remarks>
public record RecordVehicleRentalOutCommand : IRequest<VehicleRentalOutDto>
{
    public Guid VehicleId { get; init; }

    /// <summary>Optional cross-reference to a tracked customer.</summary>
    public Guid? CustomerId { get; init; }

    public string RenterName { get; init; } = null!;

    public decimal DailyRate { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    public string? Note { get; init; }
}

public class RecordVehicleRentalOutCommandValidator : AbstractValidator<RecordVehicleRentalOutCommand>
{
    // A loan-out is sometimes booked a little ahead of the day the vehicle
    // actually leaves; unlike an expense, which only ever records something
    // that already happened, this can reasonably start next week. Kept
    // narrow so a mistyped year does not slip through.
    private const int MaxFutureDays = 30;

    public RecordVehicleRentalOutCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.VehicleId).NotEmpty();

        RuleFor(x => x.RenterName)
            .NotEmpty().WithMessage("Say who has the vehicle.")
            .MaximumLength(200);

        RuleFor(x => x.DailyRate)
            .GreaterThan(0).WithMessage("A day out has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo rather than a daily rate.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A loan cannot be recorded more than {CostRules.MaxBackdatingDays} days back.")
            .LessThanOrEqualTo(today.AddDays(MaxFutureDays))
            .WithMessage($"A loan cannot start more than {MaxFutureDays} days from now.")
            .When(x => x.StartDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordVehicleRentalOutCommandHandler
    : IRequestHandler<RecordVehicleRentalOutCommand, VehicleRentalOutDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordVehicleRentalOutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleRentalOutDto> Handle(
        RecordVehicleRentalOutCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record vehicle rentals.");
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (request.CustomerId is { } customerId
            && !await _context.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), customerId);
        }

        var rental = new VehicleRentalOut
        {
            VehicleId = request.VehicleId,
            CustomerId = request.CustomerId,
            RenterName = request.RenterName.Trim(),
            DailyRate = request.DailyRate,
            StartDate = request.StartDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            Note = request.Note?.Trim(),
            SetByUserId = _currentUserService.UserId
        };

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                if (vehicle.Status != VehicleStatus.Available)
                {
                    throw new ConflictException("The vehicle is not available to rent out.");
                }

                vehicle.Status = VehicleStatus.RentedOut;

                _context.VehicleRentalsOut.Add(rental);
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
