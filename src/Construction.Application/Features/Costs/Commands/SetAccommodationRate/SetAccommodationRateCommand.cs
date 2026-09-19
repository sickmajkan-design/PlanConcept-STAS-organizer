using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.SetAccommodationRate;

/// <summary>Puts a new monthly rate in force for an accommodation, from a given date.</summary>
/// <remarks>
/// A renewed lease, expressed the way it happens: from the renewal date, this
/// apartment costs a different amount per month. The open-ended rate that was
/// in force is closed off the day before, rather than edited — editing it
/// would rewrite what last month's report said housing cost.
/// </remarks>
public record SetAccommodationRateCommand : IRequest<AccommodationRateDto>
{
    public Guid AccommodationId { get; init; }

    /// <summary>What the amount counts. Monthly unless said otherwise.</summary>
    public AccommodationChargeKind Kind { get; init; } = AccommodationChargeKind.Monthly;

    /// <summary>Per month, per person per day, or once, depending on <see cref="Kind"/>.</summary>
    public decimal Amount { get; init; }

    public string? Provider { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Null leaves it open-ended, which is the usual case.</summary>
    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }
}

public class SetAccommodationRateCommandValidator : AbstractValidator<SetAccommodationRateCommand>
{
    public SetAccommodationRateCommandValidator()
    {
        RuleFor(x => x.AccommodationId).NotEmpty();

        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("A charge has to cost something.")
            .LessThanOrEqualTo(CostRules.MaxHourlyRate)
            .WithMessage("That amount looks like a typo.");

        RuleFor(x => x.Provider).MaximumLength(200);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("The rate cannot end before it starts.")
            .When(x => x.StartDate is not null && x.EndDate is not null);

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class SetAccommodationRateCommandHandler
    : IRequestHandler<SetAccommodationRateCommand, AccommodationRateDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetAccommodationRateCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AccommodationRateDto> Handle(
        SetAccommodationRateCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not set accommodation rates.");
        }

        if (!await _context.Accommodations.AnyAsync(a => a.Id == request.AccommodationId, cancellationToken))
        {
            throw new NotFoundException(nameof(Accommodation), request.AccommodationId);
        }

        var startDate = request.StartDate ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        // A one-off charge is a single day: nothing to chain or to overlap.
        var isOneOff = request.Kind == AccommodationChargeKind.OneOff;
        var endDate = isOneOff ? startDate : request.EndDate;

        var rate = new AccommodationRate
        {
            AccommodationId = request.AccommodationId,
            Kind = request.Kind,
            Amount = request.Amount,
            Provider = request.Provider?.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Note = request.Note?.Trim(),
            SetByUserId = _currentUserService.UserId
        };

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                if (isOneOff)
                {
                    _context.AccommodationRates.Add(rate);
                    await _context.SaveChangesAsync(token);
                    return;
                }

                var predecessor = await _context.AccommodationRates
                    .Where(r => r.AccommodationId == request.AccommodationId
                        && r.Kind == request.Kind
                        && r.EndDate == null
                        && r.StartDate < startDate)
                    .OrderByDescending(r => r.StartDate)
                    .FirstOrDefaultAsync(token);

                if (predecessor is not null)
                {
                    predecessor.EndDate = startDate.AddDays(-1);
                }

                _context.AccommodationRates.Add(rate);

                var clashes = await _context.AccommodationRates
                    .AnyAsync(
                        r => r.AccommodationId == request.AccommodationId
                            && r.Kind == request.Kind
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

        return await _context.AccommodationRates
            .AsNoTracking()
            .Where(r => r.Id == rate.Id)
            .Select(AccommodationRateMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
