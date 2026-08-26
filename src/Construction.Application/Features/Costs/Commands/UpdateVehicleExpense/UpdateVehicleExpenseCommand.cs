using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.UpdateVehicleExpense;

/// <summary>Corrects a previously recorded vehicle cost.</summary>
public record UpdateVehicleExpenseCommand : IRequest<VehicleExpenseDto>
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public VehicleExpenseKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    /// <summary>Required for fuel, refused for everything else.</summary>
    public decimal? Litres { get; init; }

    public int? OdometerKm { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class UpdateVehicleExpenseCommandValidator
    : AbstractValidator<UpdateVehicleExpenseCommand>
{
    public UpdateVehicleExpenseCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("An amount cannot be negative.");

        RuleFor(x => x.Litres)
            .NotNull().WithMessage("Say how many litres went in.")
            .GreaterThan(0).WithMessage("A fill-up of nothing is not a fill-up.")
            .When(x => x.Kind == VehicleExpenseKind.Fuel);

        RuleFor(x => x.Litres)
            .Null().WithMessage("Only a fill-up has litres.")
            .When(x => x.Kind != VehicleExpenseKind.Fuel);

        RuleFor(x => x.OdometerKm)
            .GreaterThanOrEqualTo(0)
            .When(x => x.OdometerKm is not null);

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("A cost cannot be incurred in the future.")
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A cost cannot be recorded more than {CostRules.MaxBackdatingDays} days back.");

        RuleFor(x => x.Supplier).MaximumLength(200);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class UpdateVehicleExpenseCommandHandler
    : IRequestHandler<UpdateVehicleExpenseCommand, VehicleExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateVehicleExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<VehicleExpenseDto> Handle(
        UpdateVehicleExpenseCommand request,
        CancellationToken cancellationToken)
    {
        // Grouped with deletion rather than recording: a wrong figure is an
        // everyday mistake, but rewriting one after the fact is how a total
        // stops matching the paperwork behind it.
        if (!CostRules.CanDeleteSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not correct vehicle costs.");
        }

        var expense = await _context.VehicleExpenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleExpense), request.Id);

        if (!await _context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        }

        expense.VehicleId = request.VehicleId;
        expense.Kind = request.Kind;
        expense.Amount = request.Amount;
        expense.OccurredOn = request.OccurredOn;
        expense.Litres = request.Kind == VehicleExpenseKind.Fuel ? request.Litres : null;
        expense.OdometerKm = request.OdometerKm;
        expense.Supplier = request.Supplier?.Trim();
        expense.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(VehicleExpenseMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
