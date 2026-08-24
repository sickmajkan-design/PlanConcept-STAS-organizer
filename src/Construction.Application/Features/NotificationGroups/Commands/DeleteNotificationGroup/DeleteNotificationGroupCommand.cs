using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.NotificationGroups.Commands.DeleteNotificationGroup;

public record DeleteNotificationGroupCommand(Guid Id) : IRequest;

public class DeleteNotificationGroupCommandHandler : IRequestHandler<DeleteNotificationGroupCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteNotificationGroupCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteNotificationGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await _context.NotificationGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(NotificationGroup), request.Id);

        group.IsDeleted = true;
        group.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
