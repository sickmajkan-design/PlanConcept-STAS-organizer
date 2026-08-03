using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WorkItems.Commands.DeleteWorkItem;

/// <summary>Soft-deletes a work item.</summary>
/// <remarks>
/// Cancelling is the ordinary way to drop work, because it leaves a record
/// that it existed and was dropped. Deleting is for something raised by
/// mistake, which is why it stops at Admin.
/// </remarks>
public record DeleteWorkItemCommand(Guid Id) : IRequest;

public class DeleteWorkItemCommandHandler : IRequestHandler<DeleteWorkItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteWorkItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteWorkItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role is not (UserRole.SuperAdmin or UserRole.Admin))
        {
            throw new ForbiddenAccessException(
                "Only an administrator can delete work. Cancel it instead.");
        }

        var item = await _context.WorkItems
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkItem), request.Id);

        _context.WorkItems.Remove(item);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
