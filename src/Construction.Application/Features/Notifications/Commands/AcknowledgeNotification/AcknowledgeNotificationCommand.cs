using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Commands.AcknowledgeNotification;

/// <summary>
/// Confirms the current user saw one notification that required it. Idempotent
/// — acknowledging twice just leaves the first timestamp standing.
/// </summary>
public record AcknowledgeNotificationCommand(Guid Id) : IRequest;

public class AcknowledgeNotificationCommandHandler : IRequestHandler<AcknowledgeNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcknowledgeNotificationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(AcknowledgeNotificationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        if (notification.AcknowledgedAt is not null)
        {
            return;
        }

        notification.AcknowledgedAt = _dateTimeProvider.UtcNow;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = notification.AcknowledgedAt;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
