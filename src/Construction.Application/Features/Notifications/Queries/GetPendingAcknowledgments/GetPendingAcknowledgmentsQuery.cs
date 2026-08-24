using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Notifications.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Queries.GetPendingAcknowledgments;

/// <summary>
/// The current user's notifications that require explicit confirmation and
/// have not been given it — what the mobile app blocks actions on until it is
/// empty.
/// </summary>
public record GetPendingAcknowledgmentsQuery : IRequest<List<NotificationDto>>;

public class GetPendingAcknowledgmentsQueryHandler
    : IRequestHandler<GetPendingAcknowledgmentsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingAcknowledgmentsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<NotificationDto>> Handle(
        GetPendingAcknowledgmentsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        return await _context.Notifications
            .Where(n => n.UserId == userId
                && n.RequiresAcknowledgment
                && n.AcknowledgedAt == null)
            .OrderBy(n => n.CreatedAt)
            .Select(NotificationMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
