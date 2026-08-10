using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.WorkItems.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems.Commands.UpdateWorkItem;

/// <summary>Edits a work item's details. Status moves through its own command.</summary>
public record UpdateWorkItemCommand : WorkItemCommandBase, IRequest<WorkItemDto>
{
    public Guid Id { get; init; }
}

public class UpdateWorkItemCommandValidator
    : WorkItemCommandBaseValidator<UpdateWorkItemCommand>
{
    public UpdateWorkItemCommandValidator(IDateTimeProvider dateTimeProvider)
        : base(dateTimeProvider)
    {
    }
}

public class UpdateWorkItemCommandHandler
    : IRequestHandler<UpdateWorkItemCommand, WorkItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public UpdateWorkItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task<WorkItemDto> Handle(
        UpdateWorkItemCommand request,
        CancellationToken cancellationToken)
    {
        var item = await _context.WorkItems
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkItem), request.Id);

        if (!WorkItemRules.CanModify(
                _currentUserService.Role, _currentUserService.EmployeeId, item))
        {
            throw new ForbiddenAccessException("You may not change this item.");
        }

        if (item.IsFinished)
        {
            throw new ConflictException(
                "This item is finished. Reopen it before making changes.");
        }

        var reassigned = item.AssignedEmployeeId != request.AssignedEmployeeId;

        if (reassigned && !WorkItemRules.CanAssign(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not assign work to someone.");
        }

        if (request.ProjectId is { } projectId && projectId != item.ProjectId)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
        }

        if (request.AssignedEmployeeId is { } employeeId && reassigned)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == employeeId, cancellationToken);

            if (!employeeExists)
            {
                throw new NotFoundException(nameof(Employee), employeeId);
            }
        }

        item.Kind = request.Kind;
        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.ProjectId = request.ProjectId;
        item.AssignedEmployeeId = request.AssignedEmployeeId;
        item.Priority = request.Priority;
        item.Latitude = request.Latitude;
        item.Longitude = request.Longitude;

        // A moved deadline is a new deadline, so the reminder is owed again.
        if (item.DueDate != request.DueDate)
        {
            item.DueDate = request.DueDate;
            item.DueReminderSentAt = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (reassigned)
        {
            await WorkItemNotifier.NotifyAssignedAsync(
                _context, _notifications, item, cancellationToken);
        }

        return await _context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == item.Id)
            .Select(WorkItemMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
