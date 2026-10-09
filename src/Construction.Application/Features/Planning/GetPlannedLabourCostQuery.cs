using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Application.Features.Finance;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>
/// What the schedule will cost in labour, per site and month, set beside what the approved hours have
/// cost so far.
/// </summary>
/// <remarks>
/// An estimate, and said to be one: it takes the days people are posted to a site that works that day,
/// leaves out leave and public holidays, and prices each day at the rate in force with an assumed number
/// of hours. The hours people really work are priced by <see cref="ProjectLabourPricing"/>, which is what
/// the "actual" column holds, so the two columns are not priced by two sets of rules.
///
/// Pay is shown only to whoever may see pay anywhere else (<see cref="FinanceRules.CanSeePayAsync"/>).
/// </remarks>
public record GetPlannedLabourCostQuery : IRequest<PlannedLabourCostDto>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>The working day assumed for an hourly rate.</summary>
    public decimal HoursPerDay { get; init; } = 8m;

    public Guid? BranchId { get; init; }
}

public class GetPlannedLabourCostQueryValidator : AbstractValidator<GetPlannedLabourCostQuery>
{
    public GetPlannedLabourCostQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < 400)
            .WithMessage("The period can cover at most 400 days.")
            .OverridePropertyName(nameof(GetPlannedLabourCostQuery.To));
        RuleFor(x => x.HoursPerDay).InclusiveBetween(1m, 16m);
    }
}

public class PlannedLabourCostDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public decimal HoursPerDay { get; init; }

    /// <summary>The months the period touches, as <c>yyyy-MM</c>.</summary>
    public IReadOnlyList<string> Months { get; init; } = [];

    public IReadOnlyList<PlannedLabourCostProjectDto> Projects { get; init; } = [];

    /// <summary>Planned working days of people who have no rate in force: counted as days, priced at nothing.</summary>
    public int UnpricedDays { get; init; }
}

public class PlannedLabourCostProjectDto
{
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = null!;

    public IReadOnlyList<PlannedLabourCostMonthDto> Months { get; init; } = [];

    public decimal PlannedCost { get; init; }

    public decimal ActualCost { get; init; }

    public int PlannedDays { get; init; }
}

public class PlannedLabourCostMonthDto
{
    public string Month { get; init; } = null!;

    public int PlannedDays { get; init; }

    public decimal PlannedCost { get; init; }

    /// <summary>Approved hours so far, priced. Zero for a month that has not started.</summary>
    public decimal ActualCost { get; init; }
}

public class GetPlannedLabourCostQueryHandler : IRequestHandler<GetPlannedLabourCostQuery, PlannedLabourCostDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetPlannedLabourCostQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PlannedLabourCostDto> Handle(GetPlannedLabourCostQuery request, CancellationToken cancellationToken)
    {
        if (!await FinanceRules.CanSeePayAsync(_context, _currentUser, cancellationToken))
        {
            throw new ForbiddenAccessException("You may not see what people are paid.");
        }

        var from = request.From;
        var to = request.To;
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        var branchToken = request.BranchId is { } scopedBranch ? BranchTree.Token(scopedBranch) : string.Empty;

        var postings = await _context.EmployeeProjects
            .AsNoTracking()
            .Where(ep => ep.StartDate <= to && (ep.EndDate == null || ep.EndDate >= from)
                && ep.Employee.Status == EmployeeStatus.Active)
            .Where(ep => request.BranchId == null || ep.Employee.BranchPeriods.Any(b => b.Branch.Path.Contains(branchToken) && b.EndDate == null))
            .Select(ep => new
            {
                ep.EmployeeId,
                ep.ProjectId,
                ProjectName = ep.Project.Name,
                ep.Project.CountryCode,
                ep.Project.WorksSaturdays,
                ep.Project.WorksSundays,
                ep.StartDate,
                ep.EndDate,
            })
            .ToListAsync(cancellationToken);

        var employeeIds = postings.Select(p => p.EmployeeId).Distinct().ToList();

        var absences = await _context.Absences
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.Status == AbsenceStatus.Approved && a.StartDate <= to && a.EndDate >= from)
            .Select(a => new { a.EmployeeId, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var rates = await _context.EmployeeRates
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.StartDate <= to && (r.EndDate == null || r.EndDate >= from))
            .ToListAsync(cancellationToken);

        var holidays = (await _context.PublicHolidays
                .AsNoTracking()
                .Where(h => h.Date >= from && h.Date <= to)
                .Select(h => new { h.CountryCode, h.Date })
                .ToListAsync(cancellationToken))
            .Select(h => (h.CountryCode, h.Date))
            .ToHashSet();

        var planned = new Dictionary<(Guid Project, string Month), (int Days, decimal Cost)>();
        var names = new Dictionary<Guid, string>();
        var unpriced = 0;

        foreach (var person in postings.GroupBy(p => p.EmployeeId))
        {
            var mine = person.OrderBy(p => p.StartDate).ToList();
            var away = absences.Where(a => a.EmployeeId == person.Key).ToList();
            var myRates = rates.Where(r => r.EmployeeId == person.Key).ToList();

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                // The earliest posting that covers the day takes it, as on the schedule: one person, one day.
                var posting = mine.FirstOrDefault(p => p.StartDate <= day && (p.EndDate == null || p.EndDate >= day));
                if (posting is null || away.Any(a => a.StartDate <= day && a.EndDate >= day))
                {
                    continue;
                }

                var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var works = !weekend
                    || (day.DayOfWeek == DayOfWeek.Saturday && posting.WorksSaturdays)
                    || (day.DayOfWeek == DayOfWeek.Sunday && posting.WorksSundays);

                if (!works || (posting.CountryCode != null && holidays.Contains((posting.CountryCode, day))))
                {
                    continue;
                }

                var rate = myRates.FirstOrDefault(r => r.StartDate <= day && (r.EndDate == null || r.EndDate >= day));
                decimal cost;

                if (rate is null)
                {
                    unpriced++;
                    cost = 0m;
                }
                else if (rate.RateType == RateType.Daily)
                {
                    cost = rate.DailyRate ?? 0m;
                }
                else
                {
                    var hourly = weekend ? rate.WeekendHourlyRate ?? rate.HourlyRate : rate.HourlyRate;
                    cost = (hourly ?? 0m) * request.HoursPerDay;
                }

                var key = (posting.ProjectId, $"{day:yyyy-MM}");
                var now = planned.GetValueOrDefault(key);
                planned[key] = (now.Days + 1, now.Cost + cost);
                names[posting.ProjectId] = posting.ProjectName;
            }
        }

        // What the approved hours have cost so far, by the pricing the cost reports use.
        var actual = new Dictionary<(Guid Project, string Month), decimal>();

        if (from <= today)
        {
            var entries = await ProjectLabourPricing.LoadAsync(
                _context, from, to < today ? to : today, null, cancellationToken, request.BranchId);

            foreach (var entry in entries)
            {
                var key = (entry.ProjectId, $"{entry.Day:yyyy-MM}");
                actual[key] = actual.GetValueOrDefault(key) + entry.Cost;
            }

            foreach (var projectId in actual.Keys.Select(k => k.Project).Except(names.Keys).ToList())
            {
                names[projectId] = await _context.Projects.AsNoTracking().Where(p => p.Id == projectId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken) ?? "?";
            }
        }

        var months = new List<string>();
        for (var d = new DateOnly(from.Year, from.Month, 1); d <= to; d = d.AddMonths(1))
        {
            months.Add($"{d:yyyy-MM}");
        }

        var projects = names
            .OrderBy(n => n.Value)
            .Select(n =>
            {
                var cells = months
                    .Select(m => new PlannedLabourCostMonthDto
                    {
                        Month = m,
                        PlannedDays = planned.GetValueOrDefault((n.Key, m)).Days,
                        PlannedCost = Math.Round(planned.GetValueOrDefault((n.Key, m)).Cost, 2),
                        ActualCost = Math.Round(actual.GetValueOrDefault((n.Key, m)), 2),
                    })
                    .ToList();

                return new PlannedLabourCostProjectDto
                {
                    ProjectId = n.Key,
                    Name = n.Value,
                    Months = cells,
                    PlannedCost = cells.Sum(c => c.PlannedCost),
                    ActualCost = cells.Sum(c => c.ActualCost),
                    PlannedDays = cells.Sum(c => c.PlannedDays),
                };
            })
            .ToList();

        return new PlannedLabourCostDto
        {
            From = from,
            To = to,
            HoursPerDay = request.HoursPerDay,
            Months = months,
            Projects = projects,
            UnpricedDays = unpriced,
        };
    }
}
