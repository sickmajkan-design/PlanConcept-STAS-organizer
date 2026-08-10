using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.WorkItems.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems.Commands.CreateWorkItem;

/// <summary>Raises a task or reports a defect.</summary>
public record CreateWorkItemCommand : WorkItemCommandBase, IRequest<WorkItemDto>;

public class CreateWorkItemCommandValidator
    : WorkItemCommandBaseValidator<CreateWorkItemCommand>
{
    public CreateWorkItemCommandValidator(IDateTimeProvider dateTimeProvider)
        : base(dateTimeProvider)
    {
    }
}

public class CreateWorkItemCommandHandler
    : IRequestHandler<CreateWorkItemCommand, WorkItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public CreateWorkItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task<WorkItemDto> Handle(
        CreateWorkItemCommand request,
        CancellationToken cancellationToken)
    {
        if (!WorkItemRules.CanCreate(_currentUserService.Role, request.Kind))
        {
            throw new ForbiddenAccessException(
                "You may not raise work of this kind.");
        }

        if (request.AssignedEmployeeId is not null
            && !WorkItemRules.CanAssign(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not assign work to someone.");
        }

        if (request.ProjectId is { } projectId)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
        }

        if (request.AssignedEmployeeId is { } employeeId)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == employeeId, cancellationToken);

            if (!employeeExists)
            {
                throw new NotFoundException(nameof(Employee), employeeId);
            }
        }

        var item = new WorkItem
        {
            Kind = request.Kind,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            ProjectId = request.ProjectId,
            AssignedEmployeeId = request.AssignedEmployeeId,
            Priority = request.Priority,
            Status = WorkItemStatus.Open,
            DueDate = request.DueDate,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedByUserId = _currentUserService.UserId
        };

        _context.WorkItems.Add(item);

        await _context.SaveChangesAsync(cancellationToken);

        await WorkItemNotifier.NotifyAssignedAsync(
            _context, _notifications, item, cancellationToken);

        return await _context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == item.Id)
            .Select(WorkItemMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
