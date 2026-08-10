using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.WorkItems.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems.Commands.ChangeWorkItemStatus;

/// <summary>Moves a work item to another state.</summary>
/// <remarks>
/// Separate from the edit command because it is the one action a Worker
/// performs from site, and because the transitions it has to obey are a rule
/// of their own rather than a field being set.
/// </remarks>
public record ChangeWorkItemStatusCommand : IRequest<WorkItemDto>
{
    public Guid Id { get; init; }

    public WorkItemStatus Status { get; init; }
}

public class ChangeWorkItemStatusCommandValidator
    : AbstractValidator<ChangeWorkItemStatusCommand>
{
    public ChangeWorkItemStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class ChangeWorkItemStatusCommandHandler
    : IRequestHandler<ChangeWorkItemStatusCommand, WorkItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeWorkItemStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<WorkItemDto> Handle(
        ChangeWorkItemStatusCommand request,
        CancellationToken cancellationToken)
    {
        var query = _context.WorkItems.Where(w => w.Id == request.Id);

        // Narrowed before reading rather than checked after. Loading the row
        // and then refusing would answer 403 for an item the caller may not
        // see, which confirms the guessed id is real; not finding it says
        // nothing either way.
        if (WorkItemRules.IsRestrictedToOwnItems(_currentUserService.Role))
        {
            var ownEmployeeId = _currentUserService.EmployeeId;

            query = query.Where(w =>
                ownEmployeeId != null && w.AssignedEmployeeId == ownEmployeeId);
        }

        var item = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(WorkItem), request.Id);

        // Closing is the check that the work was done, so it is not the same
        // person's call as doing it — a Worker marks Resolved and stops there.
        if (request.Status == WorkItemStatus.Closed
            && !WorkItemRules.CanClose(_currentUserService.Role))
        {
            throw new ForbiddenAccessException(
                "Only a supervisor can sign work off as closed.");
        }

        WorkItemRules.EnsureTransitionAllowed(item.Status, request.Status);

        item.Status = request.Status;

        if (request.Status is WorkItemStatus.Resolved or WorkItemStatus.Closed)
        {
            item.ResolvedAt = _dateTimeProvider.UtcNow;
            item.ResolvedByUserId = _currentUserService.UserId;
        }
        else
        {
            // Reopened: the previous resolution no longer describes the item,
            // and leaving the name on it would credit someone for work that is
            // demonstrably not finished.
            item.ResolvedAt = null;
            item.ResolvedByUserId = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == item.Id)
            .Select(WorkItemMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
