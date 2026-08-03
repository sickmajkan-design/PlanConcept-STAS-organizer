using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Queries.GetTimeEntrySummary;

/// <summary>
/// Hours per employee for a period — the timesheet the office actually acts
/// on, and the number a payroll run is built from.
/// </summary>
/// <remarks>
/// Aggregated in the database rather than by paging through entries, because
/// a month of a fifty-person crew is thousands of rows to answer a question
/// whose result is fifty.
/// </remarks>
public record GetTimeEntrySummaryQuery : IRequest<TimeEntrySummaryDto>
{
    public DateTime From { get; init; }

    public DateTime To { get; init; }

    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    /// <summary>
    /// When true, counts only signed-off hours. That is the honest basis for
    /// paying anyone; the default includes pending ones so a supervisor can
    /// see what is still waiting on them.
    /// </summary>
    public bool ApprovedOnly { get; init; }
}

public class GetTimeEntrySummaryQueryValidator : AbstractValidator<GetTimeEntrySummaryQuery>
{
    /// <summary>
    /// A year at a time. Wide enough for any real timesheet question, narrow
    /// enough that one request cannot scan the whole table.
    /// </summary>
    public static readonly TimeSpan MaxRange = TimeSpan.FromDays(366);

    public GetTimeEntrySummaryQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEmpty().WithMessage("An end date is required.")
            .GreaterThan(x => x.From).WithMessage("The end of the range must be after its start.")
            .Must((query, to) => TimeEntryRules.AsUtc(to) - TimeEntryRules.AsUtc(query.From) <= MaxRange)
            .WithMessage($"The range must not exceed {MaxRange.TotalDays:0} days.")
            .When(x => x.From != default);
    }
}

public class GetTimeEntrySummaryQueryHandler
    : IRequestHandler<GetTimeEntrySummaryQuery, TimeEntrySummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTimeEntrySummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TimeEntrySummaryDto> Handle(
        GetTimeEntrySummaryQuery request,
        CancellationToken cancellationToken)
    {
        var from = TimeEntryRules.AsUtc(request.From);
        var to = TimeEntryRules.AsUtc(request.To);

        var query = _context.TimeEntries
            .AsNoTracking()
            // A running shift has no duration to add up yet.
            .Where(t => t.EndedAt != null)
            .Where(t => t.StartedAt < to && t.EndedAt > from);

        if (TimeEntryAccess.IsRestrictedToOwnEntries(_currentUserService.Role))
        {
            var ownEmployeeId = _currentUserService.EmployeeId;

            if (ownEmployeeId is null)
            {
                return new TimeEntrySummaryDto
                {
                    From = from,
                    To = to,
                    Rows = Array.Empty<TimeEntrySummaryRowDto>()
                };
            }

            query = query.Where(t => t.EmployeeId == ownEmployeeId);
        }
        else if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(t => t.EmployeeId == employeeId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(t => t.ProjectId == projectId);
        }

        if (request.ApprovedOnly)
        {
            query = query.Where(t => t.Status == TimeEntryStatus.Approved);
        }

        // Spelled out here instead of reused from TimeEntryDto because this
        // has to become SQL. PostgreSQL gives an interval for the subtraction;
        // EXTRACT(EPOCH) turns it into seconds, which Npgsql produces from
        // TotalMinutes on the resulting TimeSpan.
        var rows = await query
            .GroupBy(t => new { t.EmployeeId, t.Employee.FirstName, t.Employee.LastName })
            .Select(g => new TimeEntrySummaryRowDto
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeName = g.Key.FirstName + " " + g.Key.LastName,
                EntryCount = g.Count(),
                TotalMinutes = (int)g.Sum(t =>
                    (t.EndedAt!.Value - t.StartedAt).TotalMinutes - t.BreakMinutes),
                ApprovedMinutes = (int)g
                    .Where(t => t.Status == TimeEntryStatus.Approved)
                    .Sum(t => (t.EndedAt!.Value - t.StartedAt).TotalMinutes - t.BreakMinutes),
                PendingCount = g.Count(t => t.Status == TimeEntryStatus.Submitted)
            })
            .OrderBy(r => r.EmployeeName)
            .ToListAsync(cancellationToken);

        return new TimeEntrySummaryDto
        {
            From = from,
            To = to,
            Rows = rows
        };
    }
}
