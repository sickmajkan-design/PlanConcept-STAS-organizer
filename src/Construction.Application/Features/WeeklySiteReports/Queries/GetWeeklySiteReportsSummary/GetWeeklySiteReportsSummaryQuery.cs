using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportsSummary;

/// <summary>
/// What the office has to do about weekly reports right now: how many wait for a look, how many were sent back,
/// and which sites have not sent last week's report by the deadline.
/// </summary>
public record GetWeeklySiteReportsSummaryQuery : IRequest<WeeklySiteReportsSummaryDto>
{
    /// <summary>Only the sites of this business unit and the ones nested under it.</summary>
    public Guid? BranchId { get; init; }
}

public record WeeklySiteReportsSummaryDto
{
    public int AwaitingReview { get; init; }

    public int CorrectionRequested { get; init; }

    /// <summary>The oldest reports waiting for review, for the widget to list.</summary>
    public IReadOnlyList<WeeklySiteReportWaitingDto> Waiting { get; init; } = [];

    /// <summary>The week that is over and is being asked about.</summary>
    public int IsoYear { get; init; }

    public int IsoWeek { get; init; }

    /// <summary>When that week's reports were due.</summary>
    public DateTime DeadlineUtc { get; init; }

    /// <summary>Sites with people posted to them that have no report for the week, once the deadline has passed.</summary>
    public IReadOnlyList<WeeklySiteReportOverdueDto> Overdue { get; init; } = [];
}

public record WeeklySiteReportWaitingDto
{
    public Guid Id { get; init; }

    public string ProjectName { get; init; } = null!;

    public int IsoYear { get; init; }

    public int IsoWeek { get; init; }

    public string SubmittedByEmployeeName { get; init; } = null!;

    public DateTime CreatedAt { get; init; }
}

public record WeeklySiteReportOverdueDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    /// <summary>The foremen posted to the site, who are the ones to ask.</summary>
    public IReadOnlyList<string> ForemanNames { get; init; } = [];
}

public class GetWeeklySiteReportsSummaryQueryHandler
    : IRequestHandler<GetWeeklySiteReportsSummaryQuery, WeeklySiteReportsSummaryDto>
{
    private const int WaitingShown = 5;

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetWeeklySiteReportsSummaryQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<WeeklySiteReportsSummaryDto> Handle(
        GetWeeklySiteReportsSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var week = WeeklyReportWeek.Last(now);

        var reports = _context.WeeklySiteReports.AsNoTracking();

        if (request.BranchId is { } branchId)
        {
            var token = BranchTree.Token(branchId);
            reports = reports.Where(r => r.Project.Branch!.Path.Contains(token));
        }

        var counts = await reports
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var waiting = await reports
            .Where(r => r.Status == WeeklyReportStatus.Submitted)
            .OrderBy(r => r.CreatedAt)
            .Take(WaitingShown)
            .Select(r => new WeeklySiteReportWaitingDto
            {
                Id = r.Id,
                ProjectName = r.Project.Name,
                IsoYear = r.IsoYear,
                IsoWeek = r.IsoWeek,
                SubmittedByEmployeeName = r.SubmittedByEmployee.FirstName + " " + r.SubmittedByEmployee.LastName,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var overdue = new List<WeeklySiteReportOverdueDto>();

        if (WeeklyReportWeek.IsPastDeadline(week, now))
        {
            var missing = await WeeklyReportWeek.MissingAsync(_context, week, cancellationToken);

            if (request.BranchId is not null)
            {
                var unitSites = (await _context.Projects.AsNoTracking().InBranch(request.BranchId).Select(p => p.Id)
                    .ToListAsync(cancellationToken)).ToHashSet();
                missing = missing.Where(m => unitSites.Contains(m.ProjectId)).ToList();
            }

            var employeeIds = missing.Select(m => m.EmployeeId).Distinct().ToList();

            var foremen = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.Role == UserRole.Foreman && u.EmployeeId != null
                    && employeeIds.Contains(u.EmployeeId.Value))
                .Select(u => new { EmployeeId = u.EmployeeId!.Value, Name = u.Employee!.FirstName + " " + u.Employee.LastName })
                .ToListAsync(cancellationToken);

            var nameByEmployee = foremen.ToDictionary(f => f.EmployeeId, f => f.Name);

            overdue = missing
                .GroupBy(m => new { m.ProjectId, m.ProjectName })
                .Select(g => new WeeklySiteReportOverdueDto
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.ProjectName,
                    ForemanNames = g
                        .Where(m => nameByEmployee.ContainsKey(m.EmployeeId))
                        .Select(m => nameByEmployee[m.EmployeeId])
                        .Distinct()
                        .Order()
                        .ToList(),
                })
                .OrderBy(o => o.ProjectName)
                .ToList();
        }

        return new WeeklySiteReportsSummaryDto
        {
            AwaitingReview = counts.FirstOrDefault(c => c.Status == WeeklyReportStatus.Submitted)?.Count ?? 0,
            CorrectionRequested = counts.FirstOrDefault(c => c.Status == WeeklyReportStatus.CorrectionRequested)?.Count ?? 0,
            Waiting = waiting,
            IsoYear = week.IsoYear,
            IsoWeek = week.IsoWeek,
            DeadlineUtc = week.DeadlineUtc,
            Overdue = overdue,
        };
    }
}
