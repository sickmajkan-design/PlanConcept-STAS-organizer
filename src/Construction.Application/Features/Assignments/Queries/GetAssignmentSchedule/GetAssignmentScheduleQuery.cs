using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Assignments.Queries.GetAssignmentSchedule;

/// <summary>How a person stands today, for the colour of the dot beside their name.</summary>
public enum ScheduleDayStatus
{
    /// <summary>Not posted anywhere today and not clocked in.</summary>
    Free,

    /// <summary>Posted today, and the shift has not yet started (or has no start time and nobody has clocked in).</summary>
    Expected,

    /// <summary>Clocked in today, within the tolerance of the shift start.</summary>
    OnSite,

    /// <summary>Clocked in today, but more than the tolerance after the shift start.</summary>
    Late,

    /// <summary>Posted today, the shift start plus tolerance has passed, and nobody has clocked in.</summary>
    NoShow,

    /// <summary>Approved annual leave or other planned absence today.</summary>
    Leave,

    /// <summary>Approved sick leave today.</summary>
    Sick,
}

/// <summary>
/// The planning board's week-by-week picture: every active employee with their dated postings,
/// approved absences, held equipment and where they stand today.
/// </summary>
/// <remarks>
/// Read-only. Assigning and removing still go through the employee endpoints. A day with two
/// overlapping postings is simply two postings in the list, so a split day needs no special model.
/// </remarks>
public record GetAssignmentScheduleQuery : IRequest<AssignmentScheduleDto>
{
    /// <summary>First day shown. Defaults to the Monday of the current week.</summary>
    public DateOnly? From { get; init; }

    /// <summary>How many days to cover: 7, 14 or 28.</summary>
    public int Days { get; init; } = 14;

    public Guid? BranchId { get; init; }
}

public class AssignmentScheduleDto
{
    public DateOnly From { get; init; }

    public int Days { get; init; }

    public DateOnly Today { get; init; }

    /// <summary>Minutes after the shift start that still count as on time.</summary>
    public int LateToleranceMinutes { get; init; }

    public IReadOnlyList<ScheduleProjectDto> Projects { get; init; } = [];

    public IReadOnlyList<ScheduleEmployeeDto> Employees { get; init; } = [];
}

public class ScheduleProjectDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Status { get; init; } = null!;

    /// <summary>The site's expected clock-in time, in UTC. Null when none is set.</summary>
    public TimeOnly? ShiftStartTime { get; init; }

    public DateOnly? EndDate { get; init; }
}

public class ScheduleEmployeeDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;

    public ScheduleDayStatus Status { get; init; }

    /// <summary>When they clocked in today (UTC), if they did.</summary>
    public DateTime? ClockedInAt { get; init; }

    /// <summary>The site they clocked in at today, or the first one they are posted to today.</summary>
    public Guid? TodayProjectId { get; init; }

    public IReadOnlyList<SchedulePostingDto> Postings { get; init; } = [];

    public IReadOnlyList<ScheduleAbsenceDto> Absences { get; init; } = [];

    public IReadOnlyList<string> Vehicles { get; init; } = [];

    public IReadOnlyList<string> Tools { get; init; } = [];
}

public class SchedulePostingDto
{
    public Guid ProjectId { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }
}

public class ScheduleAbsenceDto
{
    public string Type { get; init; } = null!;

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }
}

public class GetAssignmentScheduleQueryHandler
    : IRequestHandler<GetAssignmentScheduleQuery, AssignmentScheduleDto>
{
    /// <summary>Same tolerance the time-entry screens use to flag a late clock-in.</summary>
    public static readonly TimeSpan LateTolerance = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public GetAssignmentScheduleQueryHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<AssignmentScheduleDto> Handle(
        GetAssignmentScheduleQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var days = request.Days is 7 or 14 or 28 ? request.Days : 14;
        var from = request.From ?? today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var to = from.AddDays(days - 1);

        var employees = await _context.Employees
            .AsNoTracking()
            .InBranch(request.BranchId)
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new
            {
                e.Id,
                FullName = e.FirstName + " " + e.LastName,
                e.Position,
                Postings = e.ProjectAssignments
                    .Where(a => a.StartDate <= to && (a.EndDate == null || a.EndDate >= from))
                    .Select(a => new SchedulePostingDto
                    {
                        ProjectId = a.ProjectId,
                        StartDate = a.StartDate,
                        EndDate = a.EndDate,
                    })
                    .ToList(),
                Vehicles = e.AssignedVehicles
                    .Select(v => v.Brand + " " + v.Model + " (" + v.RegistrationNumber + ")")
                    .ToList(),
                Tools = e.AssignedTools.Select(t => t.Name).ToList(),
            })
            .ToListAsync(cancellationToken);

        var ids = employees.Select(e => e.Id).ToList();

        // Approved only: a request is not yet a reason to empty a position.
        var absences = await _context.Absences
            .AsNoTracking()
            .Where(a => ids.Contains(a.EmployeeId)
                && a.Status == AbsenceStatus.Approved
                && a.StartDate <= to && a.EndDate >= from)
            .Select(a => new { a.EmployeeId, a.Type, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var dayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var entries = await _context.TimeEntries
            .AsNoTracking()
            .Where(t => ids.Contains(t.EmployeeId) && t.StartedAt >= dayStart && t.StartedAt < dayStart.AddDays(1))
            .Select(t => new { t.EmployeeId, t.ProjectId, t.StartedAt })
            .ToListAsync(cancellationToken);

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Status == ProjectStatus.Planned
                || p.Status == ProjectStatus.Active
                || p.Status == ProjectStatus.OnHold)
            .OrderBy(p => p.Name)
            .Select(p => new ScheduleProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status.ToString(),
                ShiftStartTime = p.ShiftStartTime,
                EndDate = p.EndDate,
            })
            .ToListAsync(cancellationToken);

        var shiftByProject = projects.ToDictionary(p => p.Id, p => p.ShiftStartTime);
        var timeNow = TimeOnly.FromDateTime(now);

        var rows = employees.Select(e =>
        {
            var mine = absences.Where(a => a.EmployeeId == e.Id).ToList();
            var todays = e.Postings.Where(p => p.StartDate <= today && (p.EndDate == null || p.EndDate >= today)).ToList();
            var entry = entries.Where(t => t.EmployeeId == e.Id).OrderBy(t => t.StartedAt).FirstOrDefault();
            var absentToday = mine.FirstOrDefault(a => a.StartDate <= today && a.EndDate >= today);
            var projectId = entry?.ProjectId ?? todays.FirstOrDefault()?.ProjectId;

            ScheduleDayStatus status;
            if (absentToday is not null)
            {
                status = absentToday.Type == AbsenceType.SickLeave ? ScheduleDayStatus.Sick : ScheduleDayStatus.Leave;
            }
            else if (entry is not null)
            {
                var shift = projectId is { } pid && shiftByProject.TryGetValue(pid, out var s) ? s : null;
                status = shift is { } start
                    && TimeOnly.FromDateTime(entry.StartedAt) > start.Add(LateTolerance)
                    ? ScheduleDayStatus.Late
                    : ScheduleDayStatus.OnSite;
            }
            else if (todays.Count == 0)
            {
                status = ScheduleDayStatus.Free;
            }
            else
            {
                var shifts = todays
                    .Select(p => shiftByProject.TryGetValue(p.ProjectId, out var s) ? s : null)
                    .Where(s => s is not null)
                    .ToList();

                status = shifts.Count > 0 && timeNow > shifts.Min()!.Value.Add(LateTolerance)
                    ? ScheduleDayStatus.NoShow
                    : ScheduleDayStatus.Expected;
            }

            return new ScheduleEmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                Position = e.Position,
                Status = status,
                ClockedInAt = entry?.StartedAt,
                TodayProjectId = projectId,
                Postings = e.Postings,
                Absences = mine
                    .Select(a => new ScheduleAbsenceDto { Type = a.Type.ToString(), StartDate = a.StartDate, EndDate = a.EndDate })
                    .ToList(),
                Vehicles = e.Vehicles,
                Tools = e.Tools,
            };
        }).ToList();

        return new AssignmentScheduleDto
        {
            From = from,
            Days = days,
            Today = today,
            LateToleranceMinutes = (int)LateTolerance.TotalMinutes,
            Projects = projects,
            Employees = rows,
        };
    }
}
