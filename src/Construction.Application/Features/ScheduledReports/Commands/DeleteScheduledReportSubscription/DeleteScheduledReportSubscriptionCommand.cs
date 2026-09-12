using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.ScheduledReports.Commands.DeleteScheduledReportSubscription;

public record DeleteScheduledReportSubscriptionCommand(Guid Id) : IRequest;

public class DeleteScheduledReportSubscriptionCommandHandler
    : IRequestHandler<DeleteScheduledReportSubscriptionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteScheduledReportSubscriptionCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteScheduledReportSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _context.ScheduledReportSubscriptions
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ScheduledReportSubscription), request.Id);

        // A SuperAdmin may also clear a stale subscription; anyone else only
        // ever manages their own — same "it's yours or you're SuperAdmin"
        // shape as everywhere else a personal standing order can be deleted.
        if (subscription.CreatedByUserId != _currentUserService.UserId
            && _currentUserService.Role != UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("You may not delete another user's scheduled report.");
        }

        _context.ScheduledReportSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
