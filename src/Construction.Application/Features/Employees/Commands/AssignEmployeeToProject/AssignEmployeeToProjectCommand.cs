using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;

/// <summary>Posts an employee to a project for a stretch of time.</summary>
/// <remarks>
/// The dates are optional so existing callers keep working: omitting them
/// means "from today, open-ended", which is exactly what the assignment meant
/// before it had dates.
/// </remarks>
public record AssignEmployeeToProjectCommand(Guid EmployeeId, Guid ProjectId) : IRequest
{
    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Null leaves the posting open-ended.</summary>
    public DateOnly? EndDate { get; init; }
}

public class AssignEmployeeToProjectCommandValidator
    : AbstractValidator<AssignEmployeeToProjectCommand>
{
    public AssignEmployeeToProjectCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .WithMessage("The posting cannot end before it starts.")
            .When(x => x.StartDate is not null && x.EndDate is not null);
    }
}

public class AssignEmployeeToProjectCommandHandler : IRequestHandler<AssignEmployeeToProjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;

    public AssignEmployeeToProjectCommandHandler(
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

    public async Task Handle(AssignEmployeeToProjectCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var startDate = request.StartDate
            ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var endDate = request.EndDate;

        // Overlapping postings to the same site are always a mistake. The
        // database refuses them too — this exists so the answer is a sentence
        // rather than a constraint violation, and the database exists so two
        // requests racing each other cannot both slip through.
        var overlaps = await _context.EmployeeProjects
            .AnyAsync(
                ep => ep.EmployeeId == request.EmployeeId
                    && ep.ProjectId == request.ProjectId
                    && ep.StartDate <= (endDate ?? DateOnly.MaxValue)
                    && (ep.EndDate == null || ep.EndDate >= startDate),
                cancellationToken);

        if (overlaps)
        {
            throw new ConflictException(
                "The employee is already posted to this project over those dates.");
        }

        _context.EmployeeProjects.Add(new EmployeeProject
        {
            EmployeeId = request.EmployeeId,
            ProjectId = request.ProjectId,
            StartDate = startDate,
            EndDate = endDate,
            AssignedAt = _dateTimeProvider.UtcNow,
            AssignedByUserId = _currentUserService.UserId
        });

        await _context.SaveChangesAsync(cancellationToken);

        await NotifyAsync(employee, project, cancellationToken);
    }

    private async Task NotifyAsync(Employee employee, Project project, CancellationToken cancellationToken)
    {
        var data = new Dictionary<string, string>
        {
            ["projectId"] = project.Id.ToString(),
            ["employeeId"] = employee.Id.ToString()
        };

        // The assigned employee learns about their new project.
        if (employee.User is { IsActive: true } user)
        {
            await _notificationService.NotifyUserAsync(
                user.Id,
                NotificationType.ProjectAssigned,
                "New project assigned",
                $"You have been assigned to project '{project.Name}'.",
                data,
                cancellationToken);
        }

        // Foremen and project managers already on the crew learn about the newcomer.
        var supervisorIds = await _context.Users
            .Where(u => u.IsActive &&
                        u.EmployeeId != null &&
                        u.EmployeeId != employee.Id &&
                        (u.Role == UserRole.ProjectManager || u.Role == UserRole.Foreman) &&
                        _context.EmployeeProjects.Any(ep =>
                            ep.ProjectId == project.Id && ep.EmployeeId == u.EmployeeId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await _notificationService.NotifyUsersAsync(
            supervisorIds,
            NotificationType.EmployeeAssigned,
            "Employee assigned to your project",
            $"{employee.FullName} has been assigned to project '{project.Name}'.",
            data,
            cancellationToken);
    }
}
