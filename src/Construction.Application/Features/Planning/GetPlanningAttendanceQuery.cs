using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>
/// Who has clocked in today and where they are, for the planning board: one small row per person posted to a site today or
/// with a shift today. Kept apart from <see cref="GetPlanningQuery"/> because it changes by the minute while the plan does
/// not, so the screen can ask for it alone, every minute.
/// </summary>
/// <remarks>
/// Raw facts again: how far from the site, and how long ago. Whether that is "late" or "on the wrong site" is worked out in
/// the browser, from the site's start of shift and the 100 m the rest of the system already counts as "on site".
/// </remarks>
public record GetPlanningAttendanceQuery : IRequest<PlanningAttendanceDto>
{
    public Guid? BranchId { get; init; }
}

public class PlanningAttendanceDto
{
    public DateOnly Today { get; init; }

    /// <summary>The time of day in the company's time zone at the moment of the answer, `HH:mm`.</summary>
    public string NowLocal { get; init; } = null!;

    public IReadOnlyList<PlanningAttendanceRowDto> Rows { get; init; } = [];
}

public class PlanningAttendanceRowDto
{
    public Guid EmployeeId { get; init; }

    /// <summary>The site they are posted to today. Null when they have a shift but no posting.</summary>
    public Guid? SiteId { get; init; }

    /// <summary>When today's shift started and ended, `HH:mm` in company time. Null: no shift yet / still running.</summary>
    public string? ClockedInAt { get; init; }

    public string? ClockedOutAt { get; init; }

    /// <summary>How far from the site they were when they clocked in, in whole metres. Null: no coordinates on one side.</summary>
    public int? ClockInMeters { get; init; }

    /// <summary>How long ago the last position came in today. Null: none today.</summary>
    public int? LastPingMinutesAgo { get; init; }

    public int? LastPingMeters { get; init; }

    /// <summary>The accuracy the phone reported for the last position, in metres.</summary>
    public int? LastPingAccuracy { get; init; }
}

public class GetPlanningAttendanceQueryHandler : IRequestHandler<GetPlanningAttendanceQuery, PlanningAttendanceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;

    public GetPlanningAttendanceQueryHandler(IApplicationDbContext context, IDateTimeProvider clock, ICurrentUserService currentUser)
    {
        _context = context;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<PlanningAttendanceDto> Handle(GetPlanningAttendanceQuery request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);

        // A foreman is held to their own unit whatever unit they ask for.
        var branchId = access.IsScoped ? access.BranchId : request.BranchId;

        var utcNow = _clock.UtcNow;
        var today = _clock.Today;
        var startOfDay = CompanyTime.StartOfDayUtc(today);

        // One lookup per person against the indexes of the shift and position tables: the newest of each, from today only.
        var people = await _context.Employees
            .AsNoTracking()
            .InBranch(branchId)
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => new
            {
                e.Id,
                Site = e.ProjectAssignments
                    .Where(a => a.StartDate <= today && (a.EndDate == null || a.EndDate >= today))
                    .OrderBy(a => a.StartDate)
                    .Select(a => new { a.ProjectId, a.Project.Latitude, a.Project.Longitude })
                    .FirstOrDefault(),
                Shift = e.TimeEntries
                    .Where(t => t.StartedAt >= startOfDay)
                    .OrderByDescending(t => t.StartedAt)
                    .Select(t => new { t.StartedAt, t.EndedAt, t.StartLatitude, t.StartLongitude })
                    .FirstOrDefault(),
                Ping = e.LocationRecords
                    .Where(l => l.Timestamp >= startOfDay)
                    .OrderByDescending(l => l.Timestamp)
                    .Select(l => new { l.Timestamp, l.Latitude, l.Longitude, l.Accuracy })
                    .FirstOrDefault(),
            })
            .Where(x => x.Site != null || x.Shift != null)
            .ToListAsync(cancellationToken);

        static int? Meters(double? lat, double? lon, double? siteLat, double? siteLon) =>
            lat is { } a && lon is { } b && siteLat is { } c && siteLon is { } d
                ? (int)Math.Round(GeoDistance.Meters(a, b, c, d))
                : null;

        var rows = people.Select(p => new PlanningAttendanceRowDto
        {
            EmployeeId = p.Id,
            SiteId = p.Site?.ProjectId,
            ClockedInAt = p.Shift is { } s ? CompanyTime.LocalTimeOfDay(s.StartedAt).ToString("HH:mm") : null,
            ClockedOutAt = p.Shift?.EndedAt is { } end ? CompanyTime.LocalTimeOfDay(end).ToString("HH:mm") : null,
            ClockInMeters = Meters(p.Shift?.StartLatitude, p.Shift?.StartLongitude, p.Site?.Latitude, p.Site?.Longitude),
            LastPingMinutesAgo = p.Ping is { } ping ? Math.Max(0, (int)(utcNow - ping.Timestamp).TotalMinutes) : null,
            LastPingMeters = Meters(p.Ping?.Latitude, p.Ping?.Longitude, p.Site?.Latitude, p.Site?.Longitude),
            LastPingAccuracy = p.Ping?.Accuracy is { } accuracy ? (int)Math.Round(accuracy) : null,
        }).ToList();

        return new PlanningAttendanceDto
        {
            Today = today,
            NowLocal = CompanyTime.LocalTimeOfDay(utcNow).ToString("HH:mm"),
            Rows = rows,
        };
    }
}
