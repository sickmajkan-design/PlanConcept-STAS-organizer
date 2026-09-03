using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
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

    public decimal MonthlyAmount { get; init; }

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
        var rate = new AccommodationRate
        {
            AccommodationId = request.AccommodationId,
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
                var predecessor = await _context.AccommodationRates
                    .Where(r => r.AccommodationId == request.AccommodationId
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
