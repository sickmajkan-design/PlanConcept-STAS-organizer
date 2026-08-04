using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.RecordVehicleExpense;

/// <summary>Records a tank of fuel, a service, or any other cost of a vehicle.</summary>
public record RecordVehicleExpenseCommand : IRequest<VehicleExpenseDto>
{
    public Guid VehicleId { get; init; }

    public VehicleExpenseKind Kind { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? OccurredOn { get; init; }

    /// <summary>Required for fuel, refused for everything else.</summary>
    public decimal? Litres { get; init; }

    public int? OdometerKm { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }
}

public class RecordVehicleExpenseCommandValidator
    : AbstractValidator<RecordVehicleExpenseCommand>
{
    public RecordVehicleExpenseCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("An amount cannot be negative.");

        // Mirrors the database's check constraint, so the answer is a sentence
        // on the right field rather than a constraint violation.
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
                $"A cost cannot be recorded more than {CostRules.MaxBackdatingDays} days back.")
            .When(x => x.OccurredOn is not null);

        RuleFor(x => x.Supplier).MaximumLength(200);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class RecordVehicleExpenseCommandHandler
    : IRequestHandler<RecordVehicleExpenseCommand, VehicleExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public RecordVehicleExpenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<VehicleExpenseDto> Handle(
        RecordVehicleExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record vehicle costs.");
        }

        if (!await _context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        }

        var expense = new VehicleExpense
        {
            VehicleId = request.VehicleId,
            Kind = request.Kind,
            Amount = request.Amount,
            OccurredOn = request.OccurredOn
                ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            // Belt and braces with the validator and the check constraint: a
            // future caller that skips validation still cannot put litres on
            // an insurance premium.
            Litres = request.Kind == VehicleExpenseKind.Fuel ? request.Litres : null,
            OdometerKm = request.OdometerKm,
            Supplier = request.Supplier?.Trim(),
            Note = request.Note?.Trim(),
            RecordedByUserId = _currentUserService.UserId
        };

        _context.VehicleExpenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .ProjectTo<VehicleExpenseDto>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
