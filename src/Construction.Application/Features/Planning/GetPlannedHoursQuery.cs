using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>
/// The hours the schedule planned for each site against the hours people have reported, for a period. No pay is shown,
/// so it is open to everybody who sees the schedule.
/// </summary>
/// <remarks>
/// Planned: every working day a person is posted to the site (leave and public holidays left out) at the assumed length of
/// a working day. Reported: clocked and entered hours that are not rejected, finished or still running up to now.
/// </remarks>
public record GetPlannedHoursQuery : IRequest<PlannedHoursDto>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>The working day assumed for the plan.</summary>
    public decimal HoursPerDay { get; init; } = 8m;

    public Guid? BranchId { get; init; }
}

public class GetPlannedHoursQueryValidator : AbstractValidator<GetPlannedHoursQuery>
{
    public GetPlannedHoursQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < 100)
            .WithMessage("The period can cover at most 100 days.")
            .OverridePropertyName(nameof(GetPlannedHoursQuery.To));
        RuleFor(x => x.HoursPerDay).InclusiveBetween(1m, 16m);
    }
}

public class PlannedHoursDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public decimal HoursPerDay { get; init; }

    public IReadOnlyList<PlannedHoursProjectDto> Projects { get; init; } = [];
}

public class PlannedHoursProjectDto
{
    public Guid ProjectId { get; init; }

    public string Name { get; init; } = null!;

    public int PlannedDays { get; init; }

    public decimal PlannedHours { get; init; }

    /// <summary>Hours people reported on the site, up to today.</summary>
    public decimal ReportedHours { get; init; }

    /// <summary>The part of the plan up to today, to set against what was reported.</summary>
    public decimal PlannedHoursSoFar { get; init; }
}

public class GetPlannedHoursQueryHandler : IRequestHandler<GetPlannedHoursQuery, PlannedHoursDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetPlannedHoursQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PlannedHoursDto> Handle(GetPlannedHoursQuery request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        var branchId = access.IsScoped ? access.BranchId : request.BranchId;

        var from = request.From;
        var to = request.To;
        var today = _clock.Today;
        var token = branchId is { } scoped ? BranchTree.Token(scoped) : string.Empty;

        var postings = await _context.EmployeeProjects
            .AsNoTracking()
            .Where(ep => ep.StartDate <= to && (ep.EndDate == null || ep.EndDate >= from) && ep.Employee.Status == EmployeeStatus.Active)
            .Where(ep => branchId == null || ep.Employee.BranchPeriods.Any(b => b.Branch.Path.Contains(token) && b.EndDate == null))
            .Select(ep => new { ep.EmployeeId, ep.ProjectId, ProjectName = ep.Project.Name, ep.Project.CountryCode, ep.Project.WorksSaturdays, ep.Project.WorksSundays, ep.StartDate, ep.EndDate })
            .ToListAsync(cancellationToken);

        var employeeIds = postings.Select(p => p.EmployeeId).Distinct().ToList();

        var absences = await _context.Absences.AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.Status == AbsenceStatus.Approved && a.StartDate <= to && a.EndDate >= from)
            .Select(a => new { a.EmployeeId, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var holidays = (await _context.PublicHolidays.AsNoTracking()
                .Where(h => h.Date >= from && h.Date <= to)
                .Select(h => new { h.CountryCode, h.Date })
                .ToListAsync(cancellationToken))
            .Select(h => (h.CountryCode, h.Date))
            .ToHashSet();

        var planned = new Dictionary<Guid, (int Days, int DaysSoFar)>();
        var names = new Dictionary<Guid, string>();

        foreach (var person in postings.GroupBy(p => p.EmployeeId))
        {
            var mine = person.OrderBy(p => p.StartDate).ToList();
            var away = absences.Where(a => a.EmployeeId == person.Key).ToList();

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                var posting = mine.FirstOrDefault(p => p.StartDate <= day && (p.EndDate == null || p.EndDate >= day));
                if (posting is null || away.Any(a => a.StartDate <= day && a.EndDate >= day)) continue;

                var works = (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                    || (day.DayOfWeek == DayOfWeek.Saturday && posting.WorksSaturdays)
                    || (day.DayOfWeek == DayOfWeek.Sunday && posting.WorksSundays);

                if (!works || (posting.CountryCode != null && holidays.Contains((posting.CountryCode, day)))) continue;

                var now = planned.GetValueOrDefault(posting.ProjectId);
                planned[posting.ProjectId] = (now.Days + 1, now.DaysSoFar + (day <= today ? 1 : 0));
                names[posting.ProjectId] = posting.ProjectName;
            }
        }

        var reported = new Dictionary<Guid, decimal>();

        if (from <= today)
        {
            var last = to < today ? to : today;

            var entries = await _context.TimeEntries.AsNoTracking()
                .Where(t => t.ProjectId != null && t.EndedAt != null && t.Status != TimeEntryStatus.Rejected && t.Status != TimeEntryStatus.InProgress)
                .InBranch(branchId, BranchBasis.Site)
                .Where(t => DateOnly.FromDateTime(CompanyTime.InZone(t.StartedAt, CompanyTime.ZoneId)) >= from
                    && DateOnly.FromDateTime(CompanyTime.InZone(t.StartedAt, CompanyTime.ZoneId)) <= last)
                .Select(t => new { ProjectId = t.ProjectId!.Value, Minutes = (int)((t.EndedAt!.Value - t.StartedAt).TotalMinutes - t.BreakMinutes) })
                .ToListAsync(cancellationToken);

            foreach (var entry in entries.GroupBy(e => e.ProjectId))
            {
                reported[entry.Key] = Math.Round(entry.Sum(e => Math.Max(0, e.Minutes)) / 60m, 1);
            }

            foreach (var projectId in reported.Keys.Except(names.Keys).ToList())
            {
                names[projectId] = await _context.Projects.AsNoTracking().Where(p => p.Id == projectId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken) ?? "?";
            }
        }

        var projects = names
            .OrderBy(n => n.Value)
            .Select(n =>
            {
                var p = planned.GetValueOrDefault(n.Key);

                return new PlannedHoursProjectDto
                {
                    ProjectId = n.Key,
                    Name = n.Value,
                    PlannedDays = p.Days,
                    PlannedHours = p.Days * request.HoursPerDay,
                    PlannedHoursSoFar = p.DaysSoFar * request.HoursPerDay,
                    ReportedHours = reported.GetValueOrDefault(n.Key),
                };
            })
            .ToList();

        return new PlannedHoursDto { From = from, To = to, HoursPerDay = request.HoursPerDay, Projects = projects };
    }
}
