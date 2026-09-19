using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Costs;

/// <summary>What an accommodation cost over a period, and who it was for.</summary>
public record GetAccommodationCostsQuery : IRequest<AccommodationCostSummaryDto>
{
    /// <summary>Set from the route.</summary>
    public Guid AccommodationId { get; init; }

    /// <summary>Defaults to the first of this month.</summary>
    public DateOnly? From { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? To { get; init; }
}

public class GetAccommodationCostsQueryValidator : AbstractValidator<GetAccommodationCostsQuery>
{
    public GetAccommodationCostsQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .WithMessage("The period cannot end before it starts.")
            .When(x => x.From is not null && x.To is not null);

        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.To.Value.DayNumber - x.From.Value.DayNumber < AccommodationCostCalculator.MaxDays)
            .WithMessage("That is a longer period than can be worked out at once.");
    }
}

public class GetAccommodationCostsQueryHandler
    : IRequestHandler<GetAccommodationCostsQuery, AccommodationCostSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccommodationCostsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AccommodationCostSummaryDto> Handle(
        GetAccommodationCostsQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see accommodation costs.");
        }

        if (!await _context.Accommodations.AnyAsync(a => a.Id == request.AccommodationId, cancellationToken))
        {
            throw new NotFoundException(nameof(Accommodation), request.AccommodationId);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1);
        var to = request.To ?? today;

        var rates = await _context.AccommodationRates
            .AsNoTracking()
            .Where(r => r.AccommodationId == request.AccommodationId
                && r.StartDate <= to
                && (r.EndDate == null || r.EndDate >= from))
            .ToListAsync(cancellationToken);

        var stays = await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.AccommodationId == request.AccommodationId
                && s.StartDate <= to
                && (s.EndDate == null || s.EndDate >= from))
            .ToListAsync(cancellationToken);

        var employeeIds = stays.Select(s => s.EmployeeId).Distinct().ToList();
        var projectIds = stays.Where(s => s.ProjectId != null).Select(s => s.ProjectId!.Value).Distinct().ToList();

        var employeeNames = await _context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName, cancellationToken);

        var projectNames = await _context.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return AccommodationCostCalculator.Calculate(rates, stays, from, to, employeeNames, projectNames);
    }
}
