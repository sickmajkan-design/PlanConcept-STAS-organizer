using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.FuelCards.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelCards.Commands;

/// <summary>Registers a fuel card against the vehicle it was issued with.</summary>
public record AddFuelCardCommand : IRequest<FuelCardDto>
{
    public Guid VehicleId { get; init; }

    public string Provider { get; init; } = null!;

    public string CardNumber { get; init; } = null!;

    public DateOnly? IssuedOn { get; init; }

    public string? Note { get; init; }
}

public class AddFuelCardCommandValidator : AbstractValidator<AddFuelCardCommand>
{
    public AddFuelCardCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Say which provider issued the card.")
            .MaximumLength(100);

        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("The card number is required.")
            .MaximumLength(64);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class AddFuelCardCommandHandler : IRequestHandler<AddFuelCardCommand, FuelCardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddFuelCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<FuelCardDto> Handle(
        AddFuelCardCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not manage fuel cards.");
        }

        if (!await _context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        }

        var cardNumber = request.CardNumber.Trim();

        // Mirrors VehicleUniqueness's style: a friendly 409 instead of the
        // filtered unique index's constraint violation.
        var taken = await _context.FuelCards.AnyAsync(
            c => c.CardNumber.ToLower() == cardNumber.ToLower(),
            cancellationToken);

        if (taken)
        {
            throw new ConflictException($"Card number '{cardNumber}' is already in use.");
        }

        var card = new FuelCard
        {
            VehicleId = request.VehicleId,
            Provider = request.Provider.Trim(),
            CardNumber = cardNumber,
            IssuedOn = request.IssuedOn,
            Note = request.Note?.Trim(),
        };

        _context.FuelCards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.FuelCards
            .AsNoTracking()
            .Where(c => c.Id == card.Id)
            .Select(FuelCardMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
