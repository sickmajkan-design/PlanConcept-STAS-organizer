using System.Text.Json;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>One cell of a week: who is on which site on which weekday (0 = Monday .. 4 = Friday).</summary>
public sealed record WeekPatternRow(Guid EmployeeId, int Weekday, Guid ProjectId);

/// <summary>A stretch of days one person is put on one site, as written (or, in a preview, as it would be written).</summary>
public sealed record WeekRange(Guid EmployeeId, Guid ProjectId, DateOnly From, DateOnly To);

/// <summary>What putting a week on another week did, or in a preview would do.</summary>
public sealed class WeekApplyResultDto
{
    public int Days { get; init; }

    public int People { get; init; }

    /// <summary>Cells left out because the person is on approved leave that day.</summary>
    public int SkippedOnLeave { get; init; }

    /// <summary>Cells left out because the person already has something on that day: nothing is overwritten.</summary>
    public int SkippedTaken { get; init; }

    /// <summary>Cells left out because the site is finished, not started yet or gone.</summary>
    public int SkippedSite { get; init; }

    /// <summary>The days written, so they can be taken back (all of them were free before).</summary>
    public IReadOnlyList<WeekRangeDto> Ranges { get; init; } = [];
}

public sealed class WeekRangeDto
{
    public Guid EmployeeId { get; init; }

    public Guid ProjectId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }
}

internal static class WeekPattern
{
    public static DateOnly MondayOf(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>
    /// Who is on which site on each weekday of the week. The earliest posting that covers a day takes it, as on the
    /// schedule: one person, one day. Weekends are not part of a pattern.
    /// </summary>
    public static async Task<List<WeekPatternRow>> ReadAsync(
        IApplicationDbContext context, DateOnly monday, PlanningAccess access, CancellationToken cancellationToken)
    {
        var friday = monday.AddDays(4);
        var token = access.IsScoped ? BranchTree.Token(access.BranchId!.Value) : string.Empty;

        var postings = await context.EmployeeProjects
            .AsNoTracking()
            .Where(ep => ep.StartDate <= friday && (ep.EndDate == null || ep.EndDate >= monday) && ep.Employee.Status == EmployeeStatus.Active)
            .Where(ep => !access.IsScoped || ep.Employee.BranchPeriods.Any(b => b.Branch.Path.Contains(token) && b.EndDate == null))
            .Select(ep => new { ep.EmployeeId, ep.ProjectId, ep.StartDate, ep.EndDate })
            .ToListAsync(cancellationToken);

        var rows = new List<WeekPatternRow>();

        foreach (var person in postings.GroupBy(p => p.EmployeeId))
        {
            var mine = person.OrderBy(p => p.StartDate).ToList();

            for (var weekday = 0; weekday < 5; weekday++)
            {
                var day = monday.AddDays(weekday);
                var posting = mine.FirstOrDefault(p => p.StartDate <= day && (p.EndDate == null || p.EndDate >= day));

                if (posting is not null)
                {
                    rows.Add(new WeekPatternRow(person.Key, weekday, posting.ProjectId));
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// Works out what putting the pattern on the week starting <paramref name="targetMonday"/> would write. Only free
    /// days are ever filled: a day the person already has something on, is on leave, or whose site is closed is left
    /// out and counted.
    /// </summary>
    public static async Task<(List<WeekRange> Ranges, WeekApplyResultDto Summary)> PlanAsync(
        IApplicationDbContext context, IReadOnlyList<WeekPatternRow> pattern, DateOnly targetMonday, CancellationToken cancellationToken)
    {
        var friday = targetMonday.AddDays(4);
        var employeeIds = pattern.Select(r => r.EmployeeId).Distinct().ToList();
        var projectIds = pattern.Select(r => r.ProjectId).Distinct().ToList();

        var active = (await context.Employees.AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id) && e.Status == EmployeeStatus.Active)
                .Select(e => e.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var taken = await context.EmployeeProjects.AsNoTracking()
            .Where(ep => employeeIds.Contains(ep.EmployeeId) && ep.StartDate <= friday && (ep.EndDate == null || ep.EndDate >= targetMonday))
            .Select(ep => new { ep.EmployeeId, ep.StartDate, ep.EndDate })
            .ToListAsync(cancellationToken);

        var away = await context.Absences.AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.Status == AbsenceStatus.Approved && a.StartDate <= friday && a.EndDate >= targetMonday)
            .Select(a => new { a.EmployeeId, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var sites = await context.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Status, p.StartDate, p.EndDate })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var ranges = new List<WeekRange>();
        var onLeave = 0;
        var busy = 0;
        var closed = 0;

        foreach (var person in pattern.Where(r => active.Contains(r.EmployeeId)).GroupBy(r => r.EmployeeId))
        {
            WeekRange? open = null;
            var lastWeekday = -2;

            foreach (var cell in person.OrderBy(r => r.Weekday))
            {
                var day = targetMonday.AddDays(cell.Weekday);

                if (!sites.TryGetValue(cell.ProjectId, out var site)
                    || site.Status is ProjectStatus.Completed or ProjectStatus.Cancelled
                    || (site.StartDate is { } start && day < start)
                    || (site.EndDate is { } end && day > end))
                {
                    closed++;
                    continue;
                }

                if (away.Any(a => a.EmployeeId == person.Key && a.StartDate <= day && a.EndDate >= day))
                {
                    onLeave++;
                    continue;
                }

                if (taken.Any(t => t.EmployeeId == person.Key && t.StartDate <= day && (t.EndDate == null || t.EndDate >= day)))
                {
                    busy++;
                    continue;
                }

                if (open is not null && open.ProjectId == cell.ProjectId && lastWeekday == cell.Weekday - 1)
                {
                    open = open with { To = day };
                    ranges[^1] = open;
                }
                else
                {
                    open = new WeekRange(person.Key, cell.ProjectId, day, day);
                    ranges.Add(open);
                }

                lastWeekday = cell.Weekday;
            }
        }

        var summary = new WeekApplyResultDto
        {
            Days = ranges.Sum(r => r.To.DayNumber - r.From.DayNumber + 1),
            People = ranges.Select(r => r.EmployeeId).Distinct().Count(),
            SkippedOnLeave = onLeave,
            SkippedTaken = busy,
            SkippedSite = closed,
            Ranges = ranges.Select(r => new WeekRangeDto { EmployeeId = r.EmployeeId, ProjectId = r.ProjectId, From = r.From, To = r.To }).ToList(),
        };

        return (ranges, summary);
    }

    /// <summary>Writes the planned ranges through the same command a single move uses, so history and notices are the same.</summary>
    public static async Task WriteAsync(ISender sender, IEnumerable<WeekRange> ranges, CancellationToken cancellationToken)
    {
        foreach (var range in ranges)
        {
            await sender.Send(new SetEmployeeScheduleCommand { EmployeeId = range.EmployeeId, ProjectId = range.ProjectId, From = range.From, To = range.To, OnlyFreeDays = true }, cancellationToken);
        }
    }

    /// <summary>A person who may not be moved by this caller stops the whole thing before anything is written.</summary>
    public static async Task EnsureCanMoveAllAsync(
        IApplicationDbContext context, PlanningAccess access, IEnumerable<WeekRange> ranges, CancellationToken cancellationToken)
    {
        foreach (var site in ranges.GroupBy(r => r.ProjectId))
        {
            await PlanningRules.EnsureCanMoveAsync(context, access, site.Select(r => r.EmployeeId).Distinct().ToList(), site.Key, cancellationToken);
        }
    }
}

/// <summary>Puts one week of the schedule on another: the people who are free are put where they were that week.</summary>
public record CopyScheduleWeekCommand : IRequest<WeekApplyResultDto>
{
    /// <summary>Any day of the week to copy from.</summary>
    public DateOnly FromWeek { get; init; }

    /// <summary>Any day of the week to copy to.</summary>
    public DateOnly ToWeek { get; init; }

    /// <summary>Only work out what would be written.</summary>
    public bool DryRun { get; init; }
}

public class CopyScheduleWeekCommandValidator : AbstractValidator<CopyScheduleWeekCommand>
{
    public CopyScheduleWeekCommandValidator()
    {
        RuleFor(x => x).Must(x => WeekPattern.MondayOf(x.FromWeek) != WeekPattern.MondayOf(x.ToWeek))
            .WithMessage("Pick a different week to copy to.")
            .OverridePropertyName(nameof(CopyScheduleWeekCommand.ToWeek));
    }
}

public class CopyScheduleWeekCommandHandler : IRequestHandler<CopyScheduleWeekCommand, WeekApplyResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public CopyScheduleWeekCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<WeekApplyResultDto> Handle(CopyScheduleWeekCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");

        var pattern = await WeekPattern.ReadAsync(_context, WeekPattern.MondayOf(request.FromWeek), access, cancellationToken);
        var (ranges, summary) = await WeekPattern.PlanAsync(_context, pattern, WeekPattern.MondayOf(request.ToWeek), cancellationToken);

        if (!request.DryRun)
        {
            await WeekPattern.EnsureCanMoveAllAsync(_context, access, ranges, cancellationToken);
            await WeekPattern.WriteAsync(_sender, ranges, cancellationToken);
        }

        return summary;
    }
}

/* ---- templates ---- */

public class ScheduleTemplateDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public int People { get; init; }

    public int Days { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>Templates belong to the planners who move everybody; a foreman limited to a unit does not use them.</summary>
public record GetScheduleTemplatesQuery : IRequest<IReadOnlyList<ScheduleTemplateDto>>;

public class GetScheduleTemplatesQueryHandler : IRequestHandler<GetScheduleTemplatesQuery, IReadOnlyList<ScheduleTemplateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetScheduleTemplatesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ScheduleTemplateDto>> Handle(GetScheduleTemplatesQuery request, CancellationToken cancellationToken)
    {
        PlanningRules.EnsureNotScoped(await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken));

        var all = await _context.ScheduleTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);

        return all.Select(t =>
        {
            var rows = Parse(t.Rows);

            return new ScheduleTemplateDto { Id = t.Id, Name = t.Name, People = rows.Select(r => r.EmployeeId).Distinct().Count(), Days = rows.Count, CreatedAt = t.CreatedAt };
        }).ToList();
    }

    internal static List<WeekPatternRow> Parse(string json) => JsonSerializer.Deserialize<List<WeekPatternRow>>(json, TemplateJson.Options) ?? [];
}

internal static class TemplateJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

/// <summary>Saves a week of the schedule as a template.</summary>
public record SaveScheduleTemplateCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Any day of the week to save.</summary>
    public DateOnly Week { get; init; }
}

public class SaveScheduleTemplateCommandValidator : AbstractValidator<SaveScheduleTemplateCommand>
{
    public SaveScheduleTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public class SaveScheduleTemplateCommandHandler : IRequestHandler<SaveScheduleTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SaveScheduleTemplateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(SaveScheduleTemplateCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");
        PlanningRules.EnsureNotScoped(access);

        var name = request.Name.Trim();

        if (await _context.ScheduleTemplates.AnyAsync(t => t.Name == name, cancellationToken))
        {
            throw new Construction.Application.Common.Exceptions.ValidationException([new ValidationFailure(nameof(SaveScheduleTemplateCommand.Name), "A template with this name already exists.")]);
        }

        var pattern = await WeekPattern.ReadAsync(_context, WeekPattern.MondayOf(request.Week), access, cancellationToken);

        if (pattern.Count == 0)
        {
            throw new Construction.Application.Common.Exceptions.ValidationException([new ValidationFailure(nameof(SaveScheduleTemplateCommand.Week), "Nobody is posted anywhere in that week, so there is nothing to save.")]);
        }

        var template = new ScheduleTemplate { Name = name, Rows = JsonSerializer.Serialize(pattern, TemplateJson.Options), CreatedByUserId = _currentUser.UserId };
        _context.ScheduleTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}

public record DeleteScheduleTemplateCommand(Guid Id) : IRequest;

public class DeleteScheduleTemplateCommandHandler : IRequestHandler<DeleteScheduleTemplateCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteScheduleTemplateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteScheduleTemplateCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");
        PlanningRules.EnsureNotScoped(access);

        var template = await _context.ScheduleTemplates.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ScheduleTemplate), request.Id);

        _context.ScheduleTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Puts a saved template on a week, filling only what is free.</summary>
public record ApplyScheduleTemplateCommand : IRequest<WeekApplyResultDto>
{
    public Guid TemplateId { get; init; }

    /// <summary>Any day of the week to put it on.</summary>
    public DateOnly Week { get; init; }

    public bool DryRun { get; init; }
}

public class ApplyScheduleTemplateCommandHandler : IRequestHandler<ApplyScheduleTemplateCommand, WeekApplyResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public ApplyScheduleTemplateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<WeekApplyResultDto> Handle(ApplyScheduleTemplateCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");
        PlanningRules.EnsureNotScoped(access);

        var template = await _context.ScheduleTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException(nameof(ScheduleTemplate), request.TemplateId);

        var (ranges, summary) = await WeekPattern.PlanAsync(_context, GetScheduleTemplatesQueryHandler.Parse(template.Rows), WeekPattern.MondayOf(request.Week), cancellationToken);

        if (!request.DryRun)
        {
            await WeekPattern.WriteAsync(_sender, ranges, cancellationToken);
        }

        return summary;
    }
}
