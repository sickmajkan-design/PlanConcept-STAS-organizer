using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.VehicleTolls.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.VehicleTolls.Commands.AddVehicleToll;

/// <summary>
/// Adds a new toll obligation to a vehicle — a vignette, a tunnel toll, or a
/// road-passage charge. Can optionally be recorded already paid, so an admin
/// entering a vignette they just bought does not have to add it and then
/// immediately mark it paid as two separate calls.
/// </summary>
public record AddVehicleTollCommand : IRequest<VehicleTollDto>
{
    public Guid VehicleId { get; init; }

    public VehicleTollType Type { get; init; }

    public string Country { get; init; } = null!;

    public string? RouteSegment { get; init; }

    /// <summary>When true, the toll is recorded already paid and <see cref="ValidUntil"/> is required.</summary>
    public bool MarkPaid { get; init; }

    public DateOnly? ValidUntil { get; init; }
}

public class AddVehicleTollCommandValidator : AbstractValidator<AddVehicleTollCommand>
{
    public AddVehicleTollCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();

        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);

        RuleFor(x => x.RouteSegment).MaximumLength(200);

        RuleFor(x => x.ValidUntil)
            .NotNull().WithMessage("A valid-until date is required when marking the toll paid.")
            .When(x => x.MarkPaid);

        // A vignette bought for a date already past is not a valid purchase
        // to record — see MarkVehicleTollPaidCommand for the same rule.
        RuleFor(x => x.ValidUntil)
            .Must((command, validUntil) => validUntil is null || validUntil >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Valid-until must be today or later.")
            .When(x => x.MarkPaid && x.ValidUntil is not null);
    }
}

public class AddVehicleTollCommandHandler : IRequestHandler<AddVehicleTollCommand, VehicleTollDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddVehicleTollCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleTollDto> Handle(
        AddVehicleTollCommand request,
        CancellationToken cancellationToken)
    {
        var vehicleExists = await _context.Vehicles
            .AnyAsync(v => v.Id == request.VehicleId, cancellationToken);

        if (!vehicleExists)
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        }

        var now = _dateTimeProvider.UtcNow;

        var toll = new VehicleToll
        {
            VehicleId = request.VehicleId,
            Type = request.Type,
            Country = request.Country.Trim(),
            RouteSegment = string.IsNullOrWhiteSpace(request.RouteSegment)
                ? null
                : request.RouteSegment.Trim(),
            Status = VehicleTollStatus.Unpaid,
        };

        if (request.MarkPaid)
        {
            var paidByUserId = _currentUserService.UserId
                ?? throw new ForbiddenAccessException("No signed-in user to record the payment against.");

            toll.Status = VehicleTollStatus.Paid;
            toll.ValidUntil = request.ValidUntil;
            toll.PaidByUserId = paidByUserId;
            toll.PaidAt = now;

            toll.Payments.Add(new VehicleTollPayment
            {
                ValidUntil = request.ValidUntil!.Value,
                PaidByUserId = paidByUserId,
                PaidAt = now,
            });
        }

        _context.VehicleTolls.Add(toll);

        await _context.SaveChangesAsync(cancellationToken);

        // Reloaded rather than mapped from the in-memory entity so
        // PaidByUserName is populated from the navigation property — the
        // freshly attached PaidByUserId alone has no PaidByUser loaded.
        var saved = await _context.VehicleTolls
            .AsNoTracking()
            .Include(t => t.PaidByUser)
            .FirstAsync(t => t.Id == toll.Id, cancellationToken);

        return VehicleTollMapping.ToDto(saved, DateOnly.FromDateTime(now));
    }
}
