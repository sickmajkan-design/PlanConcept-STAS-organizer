using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Queries.GetTimeEntries;

public record GetTimeEntriesQuery : ISortablePagedQuery, IRequest<PagedList<TimeEntryDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "startedAt", "endedAt", "employeeName", "status", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? EmployeeId { get; init; }

    public Guid? ProjectId { get; init; }

    public TimeEntryStatus? Status { get; init; }

    public WorkType? WorkType { get; init; }

    /// <summary>Entries that overlap this window, not only ones fully inside it.</summary>
    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    /// <summary>Only shifts still running.</summary>
    public bool? OpenOnly { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;
}

public class GetTimeEntriesQueryValidator : SortablePagedQueryValidator<GetTimeEntriesQuery>
{
    public GetTimeEntriesQueryValidator()
        : base(GetTimeEntriesQuery.AllowedSortFields)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .WithMessage("The end of the range must not be before its start.")
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetTimeEntriesQueryHandler
    : IRequestHandler<GetTimeEntriesQuery, PagedList<TimeEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTimeEntriesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<TimeEntryDto>> Handle(
        GetTimeEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.TimeEntries.AsNoTracking();

        // A worker sees their own hours and nobody else's. Enforced here
        // rather than by a route policy because the same endpoint serves both
        // the phone and the office — a Worker asking for someone else's
        // EmployeeId gets their own rows, not a 403 that confirms the id
        // exists.
        if (TimeEntryAccess.IsRestrictedToOwnEntries(_currentUserService.Role))
        {
            var ownEmployeeId = _currentUserService.EmployeeId;

            if (ownEmployeeId is null)
            {
                // A Worker account with no employee record has no hours of its
                // own, and must not fall through to seeing everyone's.
                return new PagedList<TimeEntryDto>(
                    Array.Empty<TimeEntryDto>(), 0, request.PageNumber, request.PageSize);
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

        if (request.Status is { } status)
        {
            query = query.Where(t => t.Status == status);
        }

        if (request.WorkType is { } workType)
        {
            query = query.Where(t => t.WorkType == workType);
        }

        if (request.OpenOnly == true)
        {
            query = query.Where(t => t.EndedAt == null);
        }

        // Overlap, not containment: a night shift that starts on Sunday
        // belongs in Monday's week too, and a timesheet that hid it would be
        // wrong in a way nobody notices until payday.
        if (request.From is { } from)
        {
            var fromUtc = TimeEntryRules.AsUtc(from);
            query = query.Where(t => t.EndedAt == null || t.EndedAt > fromUtc);
        }

        if (request.To is { } to)
        {
            var toUtc = TimeEntryRules.AsUtc(to);
            query = query.Where(t => t.StartedAt < toUtc);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<TimeEntryDto>.CreateAsync(
            query.Select(TimeEntryMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<TimeEntry> ApplySorting(
        IQueryable<TimeEntry> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<TimeEntry> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("endedat", false) => query.OrderBy(t => t.EndedAt),
            ("endedat", true) => query.OrderByDescending(t => t.EndedAt),
            ("employeename", false) => query
                .OrderBy(t => t.Employee.LastName).ThenBy(t => t.Employee.FirstName),
            ("employeename", true) => query
                .OrderByDescending(t => t.Employee.LastName)
                .ThenByDescending(t => t.Employee.FirstName),
            ("status", false) => query.OrderBy(t => t.Status),
            ("status", true) => query.OrderByDescending(t => t.Status),
            ("createdat", false) => query.OrderBy(t => t.CreatedAt),
            ("createdat", true) => query.OrderByDescending(t => t.CreatedAt),
            (_, false) => query.OrderBy(t => t.StartedAt),
            _ => query.OrderByDescending(t => t.StartedAt)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(t => t.Id);
    }
}
