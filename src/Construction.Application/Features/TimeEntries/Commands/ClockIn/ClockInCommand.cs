using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.ClockIn;

/// <summary>
/// Starts a shift for the signed-in employee.
/// </summary>
/// <remarks>
/// The employee always comes from the JWT, never from the payload, for the
/// same reason location reporting does: a phone must not be able to start a
/// shift in someone else's name.
/// </remarks>
public record ClockInCommand : IRequest<TimeEntryDto>
{
    public Guid? ProjectId { get; init; }

    public WorkType WorkType { get; init; } = WorkType.Regular;

    public string? Note { get; init; }

    /// <summary>Where the phone was, when it had a fix. Optional by design —
    /// a worker in a basement must still be able to start work.</summary>
    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    /// <summary>
    /// When the handset says the shift began (UTC). Null means now.
    /// </summary>
    /// <remarks>
    /// For the phone that had no signal at seven. The app records the moment
    /// locally and sends it when the network comes back, which is the only way
    /// the start of that shift can be right: the server's own clock, read at
    /// the moment the request finally arrives, would say half past nine.
    ///
    /// It is a claim by a device, so it is bounded rather than believed
    /// outright — see <see cref="TimeEntryRules.IsAcceptableDeviceTime"/> —
    /// and the entry records the gap between the two clocks, so a supervisor
    /// reviewing the timesheet can see which rows the handset stamped.
    /// </remarks>
    public DateTime? OccurredAt { get; init; }
}

public class ClockInCommandValidator : AbstractValidator<ClockInCommand>
{
    public ClockInCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.WorkType).IsInEnum();

        RuleFor(x => x.OccurredAt!.Value)
            .Must(t => TimeEntryRules.IsAcceptableDeviceTime(t, dateTimeProvider.UtcNow))
            .WithMessage(
                "The time this shift started is either in the future or more than " +
                $"{TimeEntryRules.MaxOfflineDelay.TotalHours:0} hours ago. " +
                "A supervisor has to record it.")
            .OverridePropertyName(nameof(ClockInCommand.OccurredAt))
            .When(x => x.OccurredAt is not null);

        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude is not null);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude is not null);

        // Half a position is not a position, and storing one would put a
        // marker on the null island.
        RuleFor(x => x)
            .Must(x => x.Latitude is null == x.Longitude is null)
            .WithMessage("Latitude and longitude must be supplied together.")
            // Named so the 400 response points at a field the client can
            // highlight, instead of an error with no property at all.
            .OverridePropertyName(nameof(ClockInCommand.Longitude));
    }
}

public class ClockInCommandHandler : IRequestHandler<ClockInCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;

    public ClockInCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
    }

    public async Task<TimeEntryDto> Handle(
        ClockInCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "Only accounts linked to an employee can record work time.");

        var openShift = await _context.TimeEntries
            .AsNoTracking()
            .AnyAsync(t => t.EmployeeId == employeeId && t.EndedAt == null, cancellationToken);

        if (openShift)
        {
            // The database refuses this too; catching it here turns a
            // constraint violation into a sentence the app can show.
            throw new ConflictException("You are already clocked in.");
        }

        string? projectName = null;
        var isAssignedToProject = true;

        if (request.ProjectId is { } projectId)
        {
            projectName = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Project), projectId);

            // Not refused: a foreman filling in wherever a site is short-handed
            // that day is a real, legitimate shape of this job, and refusing
            // it here would be exactly the "stuck clocked out" failure mode
            // the rest of this handler goes out of its way to avoid. Flagged
            // to the people who can tell an ordinary favour from a mistake
            // instead.
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

            isAssignedToProject = await _context.EmployeeProjects.AnyAsync(
                a => a.EmployeeId == employeeId &&
                     a.ProjectId == projectId &&
                     a.StartDate <= today &&
                     (a.EndDate == null || a.EndDate >= today),
                cancellationToken);
        }

        // The handset's moment when it sent one, this server's otherwise. The
        // validator has already refused anything outside the window, so what
        // arrives here is either now or a shift that started within the day.
        var startedAt = request.OccurredAt is { } occurred
            ? TimeEntryRules.AsUtc(occurred)
            : _dateTimeProvider.UtcNow;

        await TimeEntryRules.EnsureNoOverlapAsync(
            _context, employeeId, startedAt, null, null, cancellationToken);

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            ProjectId = request.ProjectId,
            StartedAt = startedAt,
            WorkType = request.WorkType,
            Status = TimeEntryStatus.InProgress,
            Note = request.Note?.Trim(),
            StartLatitude = request.Latitude,
            StartLongitude = request.Longitude
        };

        _context.TimeEntries.Add(entry);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The AnyAsync check above has a race window: two clock-ins fired
            // together can both pass it before either inserts. The database's
            // own unique-open-shift-per-employee index is the real backstop
            // for that window — this turns its constraint violation into the
            // same clean conflict the pre-check gives everyone else, instead
            // of an opaque 500.
            throw new ConflictException("You are already clocked in.");
        }

        if (request.ProjectId is { } notifyProjectId)
        {
            if (isAssignedToProject)
            {
                await NotifyForemenAsync(notifyProjectId, projectName!, employeeId, cancellationToken);
            }
            else
            {
                await NotifyUnassignedClockInAsync(
                    notifyProjectId, projectName!, employeeId, cancellationToken);
            }
        }

        return await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Id == entry.Id)
            .Select(TimeEntryMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// Tells the foremen posted to this site that someone just started work —
    /// the office asked for this live, and a foreman is the person actually
    /// running the site, so they are the ones who need to know without
    /// opening the app and looking.
    /// </summary>
    private async Task NotifyForemenAsync(
        Guid projectId, string projectName, Guid employeeId, CancellationToken cancellationToken)
    {
        var employeeName = await _context.Employees
            .Where(e => e.Id == employeeId)
            .Select(e => e.FirstName + " " + e.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        if (employeeName is null)
        {
            return;
        }

        var foremanUserIds = await _context.Users
            .Where(u => u.IsActive &&
                        u.Role == UserRole.Foreman &&
                        u.EmployeeId != null &&
                        u.EmployeeId != employeeId &&
                        _context.EmployeeProjects.Any(ep =>
                            ep.ProjectId == projectId && ep.EmployeeId == u.EmployeeId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var data = new Dictionary<string, string>
        {
            ["projectId"] = projectId.ToString(),
            ["employeeId"] = employeeId.ToString(),
            ["employeeName"] = employeeName,
            ["projectName"] = projectName
        };

        await _notificationService.NotifyUsersAsync(
            foremanUserIds,
            NotificationType.EmployeeClockedIn,
            "Clocked in",
            $"{employeeName} clocked in at {projectName}.",
            data,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Flags a clock-in at a site the employee has no active posting to, to
    /// the site's foremen and to management company-wide — the people who
    /// can tell whether this was covering a gap or a mistake, since the
    /// clock-in itself is deliberately never refused for it.
    /// </summary>
    private async Task NotifyUnassignedClockInAsync(
        Guid projectId, string projectName, Guid employeeId, CancellationToken cancellationToken)
    {
        var employeeName = await _context.Employees
            .Where(e => e.Id == employeeId)
            .Select(e => e.FirstName + " " + e.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        if (employeeName is null)
        {
            return;
        }

        var recipientIds = await _context.Users
            .Where(u => u.IsActive &&
                        u.EmployeeId != employeeId &&
                        (u.Role == UserRole.SuperAdmin ||
                         u.Role == UserRole.Admin ||
                         u.Role == UserRole.ProjectManager ||
                         (u.Role == UserRole.Foreman &&
                          u.EmployeeId != null &&
                          _context.EmployeeProjects.Any(ep =>
                              ep.ProjectId == projectId && ep.EmployeeId == u.EmployeeId))))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var data = new Dictionary<string, string>
        {
            ["projectId"] = projectId.ToString(),
            ["employeeId"] = employeeId.ToString(),
            ["employeeName"] = employeeName,
            ["projectName"] = projectName
        };

        await _notificationService.NotifyUsersAsync(
            recipientIds,
            NotificationType.UnassignedProjectClockIn,
            "Clock-in at an unassigned site",
            $"{employeeName} clocked in at {projectName}, but is not currently posted there.",
            data,
            cancellationToken: cancellationToken);
    }
}
