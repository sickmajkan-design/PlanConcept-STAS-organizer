using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Employees;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>Longest stretch one planning action may cover. A year and a bit: enough for a whole project.</summary>
public static class PlanningLimits
{
    public const int MaxDays = 400;
}

/// <summary>
/// Puts an employee on a project for <c>[From, To]</c>, or takes them off every project for it.
/// </summary>
/// <remarks>
/// Absolute, not additive: whatever the person was doing in those days is replaced, so sending it
/// twice leaves the same schedule. Postings the range cuts through are trimmed or split, never
/// deleted wholesale. Leave and sick days are not touched here; an absence sits on top of a posting
/// rather than replacing it, which is also why the person simply goes back to the site afterwards.
/// </remarks>
public record SetEmployeeScheduleCommand : IRequest
{
    public Guid EmployeeId { get; init; }

    /// <summary>The site to put them on. Null leaves them free for the range.</summary>
    public Guid? ProjectId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>
    /// Fill only the days the person has no posting at all, leaving what they already do alone.
    /// What "add them where they are free" means when a stand-in is free for part of the absence.
    /// </summary>
    public bool OnlyFreeDays { get; init; }
}

public class SetEmployeeScheduleCommandValidator : AbstractValidator<SetEmployeeScheduleCommand>
{
    public SetEmployeeScheduleCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < PlanningLimits.MaxDays)
            .WithMessage($"A planning action can cover at most {PlanningLimits.MaxDays} days.")
            .OverridePropertyName(nameof(SetEmployeeScheduleCommand.To));
    }
}

public class SetEmployeeScheduleCommandHandler : IRequestHandler<SetEmployeeScheduleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifications;

    public SetEmployeeScheduleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task Handle(SetEmployeeScheduleCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        Project? project = null;

        if (request.ProjectId is { } projectId)
        {
            project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
                ?? throw new NotFoundException(nameof(Project), projectId);

            if (project.Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
            {
                throw new Construction.Application.Common.Exceptions.ValidationException(
                [
                    new ValidationFailure(nameof(SetEmployeeScheduleCommand.ProjectId), "Nobody can be posted to a project that is finished or cancelled.")
                ]);
            }
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var rows = await PostingWriter.LoadAsync(_context, request.EmployeeId, request.From, request.To, cancellationToken);
        var before = rows.Select(r => PostingWriter.ToSpan(r)).ToList();
        var todayBefore = before.Where(s => s.Covers(today)).Select(s => s.ProjectId).ToHashSet();

        List<PostingSpan> after;

        if (project is null)
        {
            after = PostingRanges.Clear(before, request.From, request.To);
        }
        else if (request.OnlyFreeDays)
        {
            after = before.Select(s => s.Copy()).ToList();

            foreach (var gap in PostingRanges.FreeGaps(before, request.From, request.To))
            {
                after = PostingRanges.Add(after, project.Id, gap.From, gap.To);
            }
        }
        else
        {
            after = PostingRanges.Add(PostingRanges.Clear(before, request.From, request.To), project.Id, request.From, request.To);
        }

        PostingWriter.Apply(_context, request.EmployeeId, rows, after, _currentUser.UserId, _clock.UtcNow);

        await PostingWriter.SyncEquipmentAsync(_context, request.EmployeeId, todayBefore, after, today, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var alreadyThere = before.Any(s => s.ProjectId == project?.Id && s.Start <= request.To && (s.End is null || s.End >= request.From));

        if (project is not null && !alreadyThere)
        {
            await ProjectAssignmentNotifier.NotifyAsync(_context, _notifications, employee, project, cancellationToken);
        }
    }
}

/// <summary>Swaps what two employees are doing over <c>[From, To]</c>.</summary>
public record SwapEmployeeSchedulesCommand : IRequest
{
    public Guid EmployeeAId { get; init; }

    public Guid EmployeeBId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }
}

public class SwapEmployeeSchedulesCommandValidator : AbstractValidator<SwapEmployeeSchedulesCommand>
{
    public SwapEmployeeSchedulesCommandValidator()
    {
        RuleFor(x => x.EmployeeAId).NotEmpty();
        RuleFor(x => x.EmployeeBId).NotEmpty().NotEqual(x => x.EmployeeAId).WithMessage("Choose two different employees.");
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < PlanningLimits.MaxDays)
            .WithMessage($"A planning action can cover at most {PlanningLimits.MaxDays} days.")
            .OverridePropertyName(nameof(SwapEmployeeSchedulesCommand.To));
    }
}

public class SwapEmployeeSchedulesCommandHandler : IRequestHandler<SwapEmployeeSchedulesCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifications;

    public SwapEmployeeSchedulesCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task Handle(SwapEmployeeSchedulesCommand request, CancellationToken cancellationToken)
    {
        var a = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == request.EmployeeAId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeAId);
        var b = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == request.EmployeeBId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeBId);

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var rowsA = await PostingWriter.LoadAsync(_context, a.Id, request.From, request.To, cancellationToken);
        var rowsB = await PostingWriter.LoadAsync(_context, b.Id, request.From, request.To, cancellationToken);
        var beforeA = rowsA.Select(PostingWriter.ToSpan).ToList();
        var beforeB = rowsB.Select(PostingWriter.ToSpan).ToList();

        var segmentsA = PostingRanges.Segments(beforeA, request.From, request.To);
        var segmentsB = PostingRanges.Segments(beforeB, request.From, request.To);

        var afterA = Fill(PostingRanges.Clear(beforeA, request.From, request.To), segmentsB);
        var afterB = Fill(PostingRanges.Clear(beforeB, request.From, request.To), segmentsA);

        PostingWriter.Apply(_context, a.Id, rowsA, afterA, _currentUser.UserId, _clock.UtcNow);
        PostingWriter.Apply(_context, b.Id, rowsB, afterB, _currentUser.UserId, _clock.UtcNow);

        await PostingWriter.SyncEquipmentAsync(_context, a.Id, beforeA.Where(s => s.Covers(today)).Select(s => s.ProjectId).ToHashSet(), afterA, today, cancellationToken);
        await PostingWriter.SyncEquipmentAsync(_context, b.Id, beforeB.Where(s => s.Covers(today)).Select(s => s.ProjectId).ToHashSet(), afterB, today, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await NotifyNewSitesAsync(a, beforeA, segmentsB, cancellationToken);
        await NotifyNewSitesAsync(b, beforeB, segmentsA, cancellationToken);
    }

    private static List<PostingSpan> Fill(List<PostingSpan> spans, List<PostingSpan> segments)
    {
        foreach (var segment in segments)
        {
            spans = PostingRanges.Add(spans, segment.ProjectId, segment.Start, segment.End!.Value, segment.CustomerCompanyId);
        }

        return PostingRanges.Normalise(spans);
    }

    private async Task NotifyNewSitesAsync(
        Employee employee,
        List<PostingSpan> before,
        List<PostingSpan> incoming,
        CancellationToken cancellationToken)
    {
        foreach (var projectId in incoming.Select(s => s.ProjectId).Distinct())
        {
            if (before.Any(s => s.ProjectId == projectId))
            {
                continue;
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project is not null)
            {
                await ProjectAssignmentNotifier.NotifyAsync(_context, _notifications, employee, project, cancellationToken);
            }
        }
    }
}

/// <summary>Sets how many people of each position a project needs. Replaces whatever was there.</summary>
public record SetProjectStaffingNeedsCommand : IRequest
{
    public Guid ProjectId { get; init; }

    public List<StaffingNeedInput> Needs { get; init; } = [];
}

public record StaffingNeedInput(string Position, int Count);

public class SetProjectStaffingNeedsCommandValidator : AbstractValidator<SetProjectStaffingNeedsCommand>
{
    public SetProjectStaffingNeedsCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Needs).Must(n => n.Count <= 40).WithMessage("A project can list at most 40 positions.");
        RuleForEach(x => x.Needs).ChildRules(need =>
        {
            need.RuleFor(n => n.Position).NotEmpty().MaximumLength(100);
            need.RuleFor(n => n.Count).InclusiveBetween(0, 500);
        });
    }
}

public class SetProjectStaffingNeedsCommandHandler : IRequestHandler<SetProjectStaffingNeedsCommand>
{
    private readonly IApplicationDbContext _context;

    public SetProjectStaffingNeedsCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(SetProjectStaffingNeedsCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), request.ProjectId);
        }

        // Two lines for the same position, typed with different case or spacing, are one need.
        var wanted = request.Needs
            .Where(n => n.Count > 0)
            .GroupBy(n => PositionKey.Of(n.Position))
            .ToDictionary(g => g.Key, g => (Position: g.First().Position.Trim(), Count: g.Sum(n => n.Count)));

        var existing = await _context.ProjectStaffingNeeds
            .Where(n => n.ProjectId == request.ProjectId)
            .ToListAsync(cancellationToken);

        foreach (var row in existing)
        {
            if (wanted.Remove(PositionKey.Of(row.Position), out var next))
            {
                row.Position = next.Position;
                row.Count = next.Count;
            }
            else
            {
                _context.ProjectStaffingNeeds.Remove(row);
            }
        }

        foreach (var (_, need) in wanted)
        {
            _context.ProjectStaffingNeeds.Add(new ProjectStaffingNeed
            {
                ProjectId = request.ProjectId,
                Position = need.Position,
                Count = need.Count,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>What makes two position names the same position.</summary>
public static class PositionKey
{
    public static string Of(string position) => position.Trim().ToLowerInvariant();
}

internal static class PostingWriter
{
    public static async Task<List<EmployeeProject>> LoadAsync(
        IApplicationDbContext context, Guid employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        // One day of slack either side, so a posting that merely touches the range can be joined to it.
        var lo = from.AddDays(-1);
        var hi = to.AddDays(1);

        return await context.EmployeeProjects
            .Where(ep => ep.EmployeeId == employeeId && ep.StartDate <= hi && (ep.EndDate == null || ep.EndDate >= lo))
            .ToListAsync(cancellationToken);
    }

    public static PostingSpan ToSpan(EmployeeProject row) => new()
    {
        Id = row.Id,
        ProjectId = row.ProjectId,
        Start = row.StartDate,
        End = row.EndDate,
        CustomerCompanyId = row.CustomerCompanyId,
    };

    /// <summary>Makes the stored rows match <paramref name="spans"/>: update, delete, or create.</summary>
    public static void Apply(
        IApplicationDbContext context,
        Guid employeeId,
        List<EmployeeProject> rows,
        IReadOnlyList<PostingSpan> spans,
        Guid? userId,
        DateTime now)
    {
        var kept = spans.Where(s => s.Id is not null).ToDictionary(s => s.Id!.Value);

        foreach (var row in rows)
        {
            if (kept.TryGetValue(row.Id, out var span))
            {
                if (row.StartDate != span.Start || row.EndDate != span.End)
                {
                    row.AcknowledgedAt = null;
                }

                row.StartDate = span.Start;
                row.EndDate = span.End;
                row.CustomerCompanyId = span.CustomerCompanyId;
            }
            else
            {
                context.EmployeeProjects.Remove(row);
            }
        }

        foreach (var span in spans.Where(s => s.Id is null))
        {
            context.EmployeeProjects.Add(new EmployeeProject
            {
                EmployeeId = employeeId,
                ProjectId = span.ProjectId,
                StartDate = span.Start,
                EndDate = span.End,
                CustomerCompanyId = span.CustomerCompanyId,
                AssignedAt = now,
                AssignedByUserId = userId,
            });
        }
    }

    /// <summary>Their gear follows them to where they stand today, and lets go of where they no longer do.</summary>
    public static async Task SyncEquipmentAsync(
        IApplicationDbContext context,
        Guid employeeId,
        IReadOnlySet<Guid> todayBefore,
        IReadOnlyList<PostingSpan> after,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var todayAfter = after.Where(s => s.Covers(today)).Select(s => s.ProjectId).Distinct().ToList();

        foreach (var left in todayBefore.Except(todayAfter))
        {
            await EmployeeEquipmentSync.ReleaseFromProjectAsync(context, employeeId, left, cancellationToken);
        }

        if (todayAfter.Count > 0 && !todayBefore.SetEquals(todayAfter))
        {
            await EmployeeEquipmentSync.FollowEmployeeAsync(context, employeeId, todayAfter[0], cancellationToken);
        }
    }
}
