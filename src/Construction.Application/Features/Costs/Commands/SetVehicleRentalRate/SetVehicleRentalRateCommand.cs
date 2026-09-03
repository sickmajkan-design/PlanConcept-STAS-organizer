using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.SetVehicleRentalRate;

/// <summary>Puts a new rental/lease rate in force from a given date.</summary>
/// <remarks>
/// A renewed contract, expressed the way it happens: from the renewal date,
/// this vehicle costs a different amount per month. The open-ended rate that
/// was in force is closed off the day before, rather than edited — editing it
/// would rewrite what last month's fleet report said the van cost.
/// </remarks>
public record SetVehicleRentalRateCommand : IRequest<VehicleRentalRateDto>
{
    public Guid VehicleId { get; init; }

    public decimal MonthlyAmount { get; init; }

    public string? Provider { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Null leaves it open-ended, which is the usual case.</summary>
    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }
}

public class SetVehicleRentalRateCommandValidator : AbstractValidator<SetVehicleRentalRateCommand>
{
    public SetVehicleRentalRateCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();

        RuleFor(x => x.MonthlyAmount)
            .GreaterThan(0).WithMessage("A month has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo rather than a monthly rate.");

        RuleFor(x => x.Provider).MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("The rate cannot end before it starts.")
            .When(x => x.StartDate is not null && x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class SetVehicleRentalRateCommandHandler
    : IRequestHandler<SetVehicleRentalRateCommand, VehicleRentalRateDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetVehicleRentalRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<VehicleRentalRateDto> Handle(
        SetVehicleRentalRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not set rental rates.");
        }

        if (!await _context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        }

        var startDate = request.StartDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var rate = new VehicleRentalRate
        {
            VehicleId = request.VehicleId,
            MonthlyAmount = request.MonthlyAmount,
            Provider = request.Provider?.Trim(),
            StartDate = startDate,
            EndDate = request.EndDate,
            Note = request.Note?.Trim(),
            SetByUserId = _currentUserService.UserId
        };

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                // Close off the rate that ran up to this one, so a renewed
                // contract is one action rather than two the office has to
                // remember. Only an open-ended earlier rate: a closed one
                // already says what period it covered.
                var predecessor = await _context.VehicleRentalRates
                    .Where(r => r.VehicleId == request.VehicleId
                        && r.EndDate == null
                        && r.StartDate < startDate)
                    .OrderByDescending(r => r.StartDate)
                    .FirstOrDefaultAsync(token);

                if (predecessor is not null)
                {
                    predecessor.EndDate = startDate.AddDays(-1);
                }

                _context.VehicleRentalRates.Add(rate);

                var clashes = await _context.VehicleRentalRates
                    .AnyAsync(
                        r => r.VehicleId == request.VehicleId
                            && (predecessor == null || r.Id != predecessor.Id)
                            && r.StartDate <= (request.EndDate ?? DateOnly.MaxValue)
                            && (r.EndDate == null || r.EndDate >= startDate),
                        token);

                if (clashes)
                {
                    throw new ConflictException(
                        "Another rate already covers those dates.");
                }

                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        return await _context.VehicleRentalRates
            .AsNoTracking()
            .Where(r => r.Id == rate.Id)
            .Select(VehicleRentalRateMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
