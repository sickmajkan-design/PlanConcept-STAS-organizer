using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Costs;

public class ChargeMonthPoint
{
    public int Year { get; init; }

    public int Month { get; init; }

    public decimal Total { get; init; }

    /// <summary>The part of the month's rent nobody used.</summary>
    public decimal VacancyCost { get; init; }

    public int PersonDays { get; init; }
}

/// <summary>How one charge has played out so far: what it has cost, who it was for, how far along it is.</summary>
public class AccommodationChargeTrackingDto
{
    public Guid RateId { get; init; }

    public Guid AccommodationId { get; init; }

    public string AccommodationName { get; init; } = null!;

    public AccommodationChargeKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public DateOnly AsOf { get; init; }

    /// <summary>The charge has not begun yet.</summary>
    public bool NotStarted { get; init; }

    /// <summary>Days of the charge that have run, up to today.</summary>
    public int ElapsedDays { get; init; }

    /// <summary>Length of the charge in days; null while it has no end date.</summary>
    public int? TotalDays { get; init; }

    /// <summary>What the charge has cost from its start until today (or its end).</summary>
    public decimal ChargedToDate { get; init; }

    /// <summary>
    /// What the whole charge will have cost by its end date if the people living
    /// there now stay; null while it has no end date.
    /// </summary>
    public decimal? ProjectedTotal { get; init; }

    public decimal VacancyCost { get; init; }

    public int VacantDays { get; init; }

    public int PersonDays { get; init; }

    public IReadOnlyList<ChargeMonthPoint> Months { get; init; } = [];

    public IReadOnlyList<EmployeeCostShare> ByEmployee { get; init; } = [];

    public IReadOnlyList<ProjectCostShare> ByProject { get; init; } = [];
}

public record GetAccommodationChargeTrackingQuery(Guid RateId) : IRequest<AccommodationChargeTrackingDto>;

public class GetAccommodationChargeTrackingQueryHandler
    : IRequestHandler<GetAccommodationChargeTrackingQuery, AccommodationChargeTrackingDto>
{
    private const int MaxMonths = 120;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAccommodationChargeTrackingQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AccommodationChargeTrackingDto> Handle(
        GetAccommodationChargeTrackingQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSeeSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see accommodation costs.");
        }

        var rate = await _context.AccommodationRates
            .AsNoTracking()
            .Include(r => r.Accommodation)
            .FirstOrDefaultAsync(r => r.Id == request.RateId, cancellationToken)
            ?? throw new NotFoundException(nameof(AccommodationRate), request.RateId);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var accommodation = rate.Accommodation;
        var name = string.IsNullOrWhiteSpace(accommodation.Name) ? accommodation.Address : accommodation.Name;

        var isOneOff = rate.Kind == AccommodationChargeKind.OneOff;
        var start = rate.StartDate;
        var end = isOneOff ? rate.StartDate : rate.EndDate;

        var dto = new AccommodationChargeTrackingDto
        {
            RateId = rate.Id,
            AccommodationId = accommodation.Id,
            AccommodationName = name,
            Kind = rate.Kind,
            Amount = rate.Amount,
            StartDate = rate.StartDate,
            EndDate = isOneOff ? null : rate.EndDate,
            AsOf = today,
            NotStarted = start > today,
            TotalDays = end is { } e ? e.DayNumber - start.DayNumber + 1 : null
        };

        if (start > today)
        {
            return dto;
        }

        var trackedTo = end is { } last && last < today ? last : today;
        var trackedFrom = start;

        if (trackedTo.DayNumber - trackedFrom.DayNumber >= AccommodationCostCalculator.MaxDays)
        {
            trackedFrom = trackedTo.AddDays(-(AccommodationCostCalculator.MaxDays - 1));
        }

        var stays = await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.AccommodationId == accommodation.Id
                && s.StartDate <= (end ?? today.AddDays(AccommodationCostCalculator.MaxDays))
                && (s.EndDate == null || s.EndDate >= trackedFrom))
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

        // Only this charge counts here, not the other rates of the same place.
        IReadOnlyCollection<AccommodationRate> only = [rate];

        var summary = AccommodationCostCalculator.Calculate(only, stays, trackedFrom, trackedTo, employeeNames, projectNames);

        decimal? projected = null;

        if (isOneOff)
        {
            projected = rate.Amount;
        }
        else if (end is { } finish
            && finish.DayNumber - start.DayNumber < AccommodationCostCalculator.MaxDays)
        {
            projected = AccommodationCostCalculator
                .Calculate(only, stays, start, finish, employeeNames, projectNames).Total;
        }

        var months = new List<ChargeMonthPoint>();

        if (!isOneOff)
        {
            var cursor = new DateOnly(trackedFrom.Year, trackedFrom.Month, 1);

            while (cursor <= trackedTo && months.Count < MaxMonths)
            {
                var monthStart = cursor < trackedFrom ? trackedFrom : cursor;
                var monthEndFull = cursor.AddMonths(1).AddDays(-1);
                var monthEnd = monthEndFull > trackedTo ? trackedTo : monthEndFull;

                var month = AccommodationCostCalculator.Calculate(only, stays, monthStart, monthEnd, employeeNames, projectNames);

                months.Add(new ChargeMonthPoint
                {
                    Year = cursor.Year,
                    Month = cursor.Month,
                    Total = month.Total,
                    VacancyCost = month.VacancyCost,
                    PersonDays = month.OccupiedPersonDays
                });

                cursor = cursor.AddMonths(1);
            }
        }

        return new AccommodationChargeTrackingDto
        {
            RateId = dto.RateId,
            AccommodationId = dto.AccommodationId,
            AccommodationName = dto.AccommodationName,
            Kind = dto.Kind,
            Amount = dto.Amount,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            AsOf = today,
            NotStarted = false,
            ElapsedDays = trackedTo.DayNumber - start.DayNumber + 1,
            TotalDays = dto.TotalDays,
            ChargedToDate = summary.Total,
            ProjectedTotal = projected,
            VacancyCost = summary.VacancyCost,
            VacantDays = summary.VacantDays,
            PersonDays = summary.OccupiedPersonDays,
            Months = months,
            ByEmployee = summary.ByEmployee,
            ByProject = summary.ByProject
        };
    }
}
