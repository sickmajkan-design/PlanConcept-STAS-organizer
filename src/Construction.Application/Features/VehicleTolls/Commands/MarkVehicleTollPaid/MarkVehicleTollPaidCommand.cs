using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.VehicleTolls.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.VehicleTolls.Commands.MarkVehicleTollPaid;

/// <summary>
/// Marks a toll paid — the initial payment, or a renewal of one already
/// paid. Either way the toll's current state is copied into a
/// <see cref="VehicleTollPayment"/> row before it is overwritten, so the
/// history of every payment survives even though the toll itself only ever
/// shows its current validity.
/// </summary>
public record MarkVehicleTollPaidCommand : IRequest<VehicleTollDto>
{
    /// <summary>Set by the API layer from the route, never from the request body.</summary>
    public Guid VehicleTollId { get; init; }

    public DateOnly ValidUntil { get; init; }
}

public class MarkVehicleTollPaidCommandValidator : AbstractValidator<MarkVehicleTollPaidCommand>
{
    public MarkVehicleTollPaidCommandValidator()
    {
        RuleFor(x => x.VehicleTollId).NotEmpty();

        RuleFor(x => x.ValidUntil)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Valid-until must be today or later.");
    }
}

public class MarkVehicleTollPaidCommandHandler
    : IRequestHandler<MarkVehicleTollPaidCommand, VehicleTollDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkVehicleTollPaidCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleTollDto> Handle(
        MarkVehicleTollPaidCommand request,
        CancellationToken cancellationToken)
    {
        var paidByUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("No signed-in user to record the payment against.");

        var toll = await _context.VehicleTolls
            .FirstOrDefaultAsync(t => t.Id == request.VehicleTollId, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleToll), request.VehicleTollId);

        var now = _dateTimeProvider.UtcNow;

        toll.Status = VehicleTollStatus.Paid;
        toll.ValidUntil = request.ValidUntil;
        toll.PaidByUserId = paidByUserId;
        toll.PaidAt = now;

        // The history row this payment/renewal leaves behind — appended,
        // never edited, regardless of whether this is the first payment or
        // the fifth renewal.
        _context.VehicleTollPayments.Add(new VehicleTollPayment
        {
            VehicleTollId = toll.Id,
            ValidUntil = request.ValidUntil,
            PaidByUserId = paidByUserId,
            PaidAt = now,
        });

        await _context.SaveChangesAsync(cancellationToken);

        var saved = await _context.VehicleTolls
            .AsNoTracking()
            .Include(t => t.PaidByUser)
            .FirstAsync(t => t.Id == toll.Id, cancellationToken);

        return VehicleTollMapping.ToDto(saved, DateOnly.FromDateTime(now));
    }
}
