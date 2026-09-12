using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Commands.SendDirectNotification;

/// <summary>
/// Sends a free-typed message straight to one employee — the office telling a
/// worker to do something right now, rather than waiting for the next
/// business event to fire a system notification.
/// </summary>
/// <remarks>
/// Unlike <see cref="Features.WorkItems.WorkItemNotifier"/>'s employee→user
/// resolution, a missing login here is refused rather than silently skipped:
/// there the notification is a side effect of another action succeeding, but
/// here the notification <em>is</em> the whole point of the call, so a caller
/// who typed a message that goes nowhere needs to be told.
/// </remarks>
public record SendDirectNotificationCommand : IRequest
{
    public Guid EmployeeId { get; init; }

    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;

    /// <summary>
    /// When true, the recipient cannot act on anything else in the app until
    /// they explicitly confirm they saw this.
    /// </summary>
    public bool RequiresAcknowledgment { get; init; }
}

public class SendDirectNotificationCommandValidator : AbstractValidator<SendDirectNotificationCommand>
{
    public SendDirectNotificationCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000);
    }
}

public class SendDirectNotificationCommandHandler : IRequestHandler<SendDirectNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendDirectNotificationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(SendDirectNotificationCommand request, CancellationToken cancellationToken)
    {
        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        // Foreman and above may compose a direct message; a Foreman's target
        // must currently be on one of the Foreman's own sites — the same
        // "narrower than the route policy" layering CostRules already uses
        // elsewhere in this codebase, since the route itself only checks
        // ForemanAndAbove.
        if (_currentUserService.Role is UserRole.Foreman)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
            var callerEmployeeId = _currentUserService.EmployeeId;

            var callerProjectIds = await _context.EmployeeProjects
                .Where(ep => ep.EmployeeId == callerEmployeeId
                    && ep.StartDate <= today
                    && (ep.EndDate == null || ep.EndDate >= today))
                .Select(ep => ep.ProjectId)
                .ToListAsync(cancellationToken);

            var targetSharesASite = await _context.EmployeeProjects
                .AnyAsync(ep => ep.EmployeeId == request.EmployeeId
                    && callerProjectIds.Contains(ep.ProjectId)
                    && ep.StartDate <= today
                    && (ep.EndDate == null || ep.EndDate >= today),
                    cancellationToken);

            if (!targetSharesASite)
            {
                throw new ForbiddenAccessException(
                    "You may only notify someone currently posted to one of your own sites.");
            }
        }

        var userId = await _context.Users
            .Where(u => u.EmployeeId == request.EmployeeId && u.IsActive)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            throw new ConflictException("This employee has no app account to notify.");
        }

        await _notifications.NotifyUserAsync(
            userId.Value,
            NotificationType.DirectMessage,
            request.Title.Trim(),
            request.Body.Trim(),
            data: null,
            request.RequiresAcknowledgment,
            cancellationToken);
    }
}
