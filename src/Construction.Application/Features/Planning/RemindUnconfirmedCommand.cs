using Construction.Application.Common;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Planning;

/// <summary>Tells the people posted on a day where they go: when a planner publishes the day, or reminds those who have not confirmed.</summary>
internal static class ScheduleNotices
{
    /// <returns>How many people were sent a notice.</returns>
    public static async Task<int> SendAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        PlanningAccess access,
        DateOnly day,
        bool onlyUnconfirmed,
        CancellationToken cancellationToken)
    {
        var token = access.IsScoped ? BranchTree.Token(access.BranchId!.Value) : string.Empty;

        var rows = await context.EmployeeProjects.AsNoTracking()
            .Where(ep => ep.StartDate <= day && (ep.EndDate == null || ep.EndDate >= day))
            .Where(ep => !onlyUnconfirmed || ep.AcknowledgedAt == null)
            .Where(ep => ep.Employee.Status == EmployeeStatus.Active && ep.Employee.User != null && ep.Employee.User.IsActive)
            .Where(ep => ep.Project.Status != ProjectStatus.Completed && ep.Project.Status != ProjectStatus.Cancelled)
            .Where(ep => !context.Absences.Any(a => a.EmployeeId == ep.EmployeeId && a.Status == AbsenceStatus.Approved && a.StartDate <= day && a.EndDate >= day))
            .Where(ep => !access.IsScoped || ep.Employee.BranchPeriods.Any(b => b.Branch.Path.Contains(token) && b.EndDate == null))
            .Select(ep => new
            {
                UserId = ep.Employee.User!.Id,
                ep.EmployeeId,
                EmployeeName = ep.Employee.FirstName + " " + ep.Employee.LastName,
                ep.StartDate,
                ProjectId = ep.Project.Id,
                ProjectName = ep.Project.Name,
                ep.Project.Address,
                ep.Project.ShiftStartTime,
            })
            .ToListAsync(cancellationToken);

        // One notice per person: the earliest posting that covers the day takes it, as on the schedule.
        var perPerson = rows.GroupBy(r => r.UserId).Select(g => g.OrderBy(r => r.StartDate).First()).ToList();

        foreach (var row in perPerson)
        {
            var data = new Dictionary<string, string>
            {
                ["projectId"] = row.ProjectId.ToString(),
                ["employeeId"] = row.EmployeeId.ToString(),
                ["projectName"] = row.ProjectName,
                ["employeeName"] = row.EmployeeName,
                ["date"] = day.ToString("yyyy-MM-dd"),
                [onlyUnconfirmed ? "reminder" : "published"] = "schedule",
            };

            var details = new List<string>();

            if (!string.IsNullOrWhiteSpace(row.Address))
            {
                data["projectAddress"] = row.Address;
                details.Add($"at {row.Address}");
            }

            if (row.ShiftStartTime is { } start)
            {
                // Stored as UTC: said to people on the company's own clock.
                var local = CompanyTime.LocalTimeOfDay(day.ToDateTime(start, DateTimeKind.Utc));
                data["projectShiftStartTime"] = local.ToString("HH:mm");
                details.Add($"the shift starts at {local:HH:mm}");
            }

            var where = $"'{row.ProjectName}'" + (details.Count > 0 ? ", " + string.Join(", ", details) : string.Empty);

            await notifications.NotifyUserAsync(
                row.UserId,
                NotificationType.ProjectAssigned,
                onlyUnconfirmed ? "Please confirm your schedule" : $"Your schedule for {day:dd.MM.yyyy}",
                onlyUnconfirmed
                    ? $"Your schedule for {day:dd.MM.yyyy} is waiting for you to confirm it: {where}."
                    : $"On {day:dd.MM.yyyy} you are posted to {where}.",
                data,
                cancellationToken: cancellationToken);
        }

        return perPerson.Count;
    }
}

/// <summary>
/// Reminds the people posted on a day who have not yet confirmed their posting to look at it and confirm. One notice per
/// person, however many postings they have.
/// </summary>
public record RemindUnconfirmedPostingsCommand : IRequest<int>
{
    public DateOnly Date { get; init; }
}

public class RemindUnconfirmedPostingsCommandHandler : IRequestHandler<RemindUnconfirmedPostingsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public RemindUnconfirmedPostingsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    /// <returns>How many people were reminded.</returns>
    public async Task<int> Handle(RemindUnconfirmedPostingsCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");

        return await ScheduleNotices.SendAsync(_context, _notifications, access, request.Date, onlyUnconfirmed: true, cancellationToken);
    }
}

/// <summary>
/// Publishes a day of the schedule: everybody posted that day (and not on approved leave) is sent where they go and, if the
/// site has one, when the shift starts. Nothing about the schedule changes; it is already live, publishing is telling people.
/// </summary>
public record PublishScheduleDayCommand : IRequest<SchedulePublicationDto>
{
    public DateOnly Date { get; init; }
}

public class PublishScheduleDayCommandValidator : AbstractValidator<PublishScheduleDayCommand>
{
    public PublishScheduleDayCommandValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.Date).Must(d => d >= clock.Today).WithMessage("A day that has passed cannot be published.");
        RuleFor(x => x.Date).Must(d => d <= clock.Today.AddDays(60)).WithMessage("Publish at most 60 days ahead.");
    }
}

public class SchedulePublicationDto
{
    public DateOnly Date { get; init; }

    /// <summary>When it was published, UTC.</summary>
    public DateTime PublishedAt { get; init; }

    public int People { get; init; }
}

public class PublishScheduleDayCommandHandler : IRequestHandler<PublishScheduleDayCommand, SchedulePublicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _clock;

    public PublishScheduleDayCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, INotificationService notifications, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
        _clock = clock;
    }

    public async Task<SchedulePublicationDto> Handle(PublishScheduleDayCommand request, CancellationToken cancellationToken)
    {
        var access = await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);
        if (!access.CanEdit) throw new ForbiddenAccessException("You may not change the schedule.");

        var people = await ScheduleNotices.SendAsync(_context, _notifications, access, request.Date, onlyUnconfirmed: false, cancellationToken);

        var row = new SchedulePublication { Date = request.Date, PublishedByUserId = _currentUser.UserId, People = people };
        _context.SchedulePublications.Add(row);
        await _context.SaveChangesAsync(cancellationToken);

        return new SchedulePublicationDto { Date = row.Date, PublishedAt = row.CreatedAt == default ? _clock.UtcNow : row.CreatedAt, People = people };
    }
}

/// <summary>When each day was last published, for a period.</summary>
public record GetSchedulePublicationsQuery : IRequest<IReadOnlyList<SchedulePublicationDto>>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }
}

public class GetSchedulePublicationsQueryValidator : AbstractValidator<GetSchedulePublicationsQuery>
{
    public GetSchedulePublicationsQueryValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("The end cannot be before the start.");
        RuleFor(x => x).Must(x => x.To.DayNumber - x.From.DayNumber < 100).WithMessage("The period can cover at most 100 days.").OverridePropertyName(nameof(GetSchedulePublicationsQuery.To));
    }
}

public class GetSchedulePublicationsQueryHandler : IRequestHandler<GetSchedulePublicationsQuery, IReadOnlyList<SchedulePublicationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSchedulePublicationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SchedulePublicationDto>> Handle(GetSchedulePublicationsQuery request, CancellationToken cancellationToken)
    {
        await PlanningRules.ResolveAsync(_context, _currentUser, cancellationToken);

        var all = await _context.SchedulePublications.AsNoTracking()
            .Where(p => p.Date >= request.From && p.Date <= request.To)
            .Select(p => new SchedulePublicationDto { Date = p.Date, PublishedAt = p.CreatedAt, People = p.People })
            .ToListAsync(cancellationToken);

        // The last time a day was published is the one that counts.
        return all.GroupBy(p => p.Date).Select(g => g.OrderByDescending(p => p.PublishedAt).First()).OrderBy(p => p.Date).ToList();
    }
}
