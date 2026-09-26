using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.ArticleOrders.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.ArticleOrders.Commands.SetArticleOrderStatus;

/// <summary>
/// Moves a request one step on: ordered, on its way, delivered, declined, or
/// withdrawn. The steps go in order and none is skipped.
/// </summary>
public record SetArticleOrderStatusCommand : IRequest<ArticleOrderDto>
{
    public Guid Id { get; init; }

    public ArticleOrderStatus Status { get; init; }

    /// <summary>Required when declining, so the person knows why.</summary>
    public string? Note { get; init; }
}

public class SetArticleOrderStatusCommandValidator : AbstractValidator<SetArticleOrderStatusCommand>
{
    public SetArticleOrderStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum().NotEqual(ArticleOrderStatus.Requested);

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason is required when declining a request.")
            .When(x => x.Status == ArticleOrderStatus.Rejected);

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class SetArticleOrderStatusCommandHandler : IRequestHandler<SetArticleOrderStatusCommand, ArticleOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public SetArticleOrderStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<ArticleOrderDto> Handle(
        SetArticleOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("Sign in to change a request.");

        var order = await _context.ArticleOrders
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ArticleOrder), request.Id);

        var manages = ArticleOrderRules.CanManage(_currentUserService.Role);
        var isRequester = order.RequestedByUserId == userId;

        // Receiving and withdrawing are the requester's own steps. The office may
        // also mark it delivered, for the person who has no phone at hand.
        var allowed = request.Status switch
        {
            ArticleOrderStatus.Cancelled => isRequester,
            ArticleOrderStatus.Delivered => isRequester || manages,
            _ => manages
        };

        if (!allowed)
        {
            throw new ForbiddenAccessException("You may not make this change to the request.");
        }

        if (!ArticleOrderRules.CanMove(order.Status, request.Status))
        {
            throw new ConflictException(
                $"The request is {order.Status} and cannot go to {request.Status}. Reload it and try again.");
        }

        var now = _dateTimeProvider.UtcNow;

        order.Status = request.Status;

        switch (request.Status)
        {
            case ArticleOrderStatus.Ordered:
                order.OrderedAt = now;
                order.HandledByUserId = userId;
                break;
            case ArticleOrderStatus.InDelivery:
                order.ShippedAt = now;
                order.HandledByUserId = userId;
                break;
            case ArticleOrderStatus.Delivered:
                order.DeliveredAt = now;
                break;
            case ArticleOrderStatus.Rejected:
                order.ReviewNote = request.Note!.Trim();
                order.HandledByUserId = userId;
                break;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This request was changed by someone else just now. Reload it and try again.");
        }

        await NotifyAsync(order, userId, cancellationToken);

        return await _context.ArticleOrders
            .AsNoTracking()
            .Where(o => o.Id == order.Id)
            .Select(ArticleOrderMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// The person who asked is told at every step; the office is told when the
    /// person has it, or withdrew, since those are not steps the office took.
    /// </summary>
    private async Task NotifyAsync(ArticleOrder order, Guid actorUserId, CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid> { order.RequestedByUserId };

        if (order.Status is ArticleOrderStatus.Delivered or ArticleOrderStatus.Cancelled)
        {
            var office = await _context.Users
                .Where(u => u.IsActive
                    && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin || u.Role == UserRole.ProjectManager))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            recipients.UnionWith(office);
        }

        recipients.Remove(actorUserId);

        if (recipients.Count == 0)
        {
            return;
        }

        var requestedBy = await _context.ArticleOrders
            .Where(o => o.Id == order.Id)
            .Select(o => o.Employee != null
                ? o.Employee.FirstName + " " + o.Employee.LastName
                : o.RequestedByUser.Email)
            .FirstAsync(cancellationToken);

        await _notifications.NotifyUsersAsync(
            recipients.ToList(),
            NotificationType.ArticleOrderStatusChanged,
            "Request for articles",
            $"The request from {requestedBy} is now {order.Status}.",
            new Dictionary<string, string>
            {
                ["articleOrderId"] = order.Id.ToString(),
                ["status"] = order.Status.ToString(),
                ["requestedByName"] = requestedBy,
                ["note"] = order.ReviewNote ?? string.Empty
            },
            cancellationToken: cancellationToken);
    }
}
