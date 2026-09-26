using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.ArticleOrders.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.ArticleOrders.Commands.CreateArticleOrder;

/// <summary>Asks for articles the person needs for the job.</summary>
public record CreateArticleOrderCommand : IRequest<ArticleOrderDto>
{
    public Guid? ProjectId { get; init; }

    public bool Urgent { get; init; }

    public string? Note { get; init; }

    public IReadOnlyList<CreateArticleOrderItem> Items { get; init; } = [];
}

public record CreateArticleOrderItem
{
    public string Name { get; init; } = null!;

    public decimal Quantity { get; init; } = 1;

    public string? Unit { get; init; }

    public string? Note { get; init; }
}

public class CreateArticleOrderCommandValidator : AbstractValidator<CreateArticleOrderCommand>
{
    public CreateArticleOrderCommandValidator()
    {
        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Add at least one article.")
            .Must(items => items.Count <= ArticleOrderRules.MaxItems)
            .WithMessage($"A request may have at most {ArticleOrderRules.MaxItems} articles.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Quantity).GreaterThan(0).LessThanOrEqualTo(100000);
            item.RuleFor(i => i.Unit).MaximumLength(30);
            item.RuleFor(i => i.Note).MaximumLength(300);
        });
    }
}

public class CreateArticleOrderCommandHandler : IRequestHandler<CreateArticleOrderCommand, ArticleOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public CreateArticleOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task<ArticleOrderDto> Handle(
        CreateArticleOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("Sign in to ask for articles.");

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var order = new ArticleOrder
        {
            RequestedByUserId = userId,
            EmployeeId = _currentUserService.EmployeeId,
            ProjectId = request.ProjectId,
            Urgent = request.Urgent,
            Note = request.Note?.Trim(),
            Items = request.Items
                .Select(i => new ArticleOrderItem
                {
                    Name = i.Name.Trim(),
                    Quantity = i.Quantity,
                    Unit = string.IsNullOrWhiteSpace(i.Unit) ? null : i.Unit.Trim(),
                    Note = string.IsNullOrWhiteSpace(i.Note) ? null : i.Note.Trim(),
                })
                .ToList(),
        };

        _context.ArticleOrders.Add(order);

        await _context.SaveChangesAsync(cancellationToken);

        var dto = await _context.ArticleOrders
            .AsNoTracking()
            .Where(o => o.Id == order.Id)
            .Select(ArticleOrderMapping.Projection)
            .FirstAsync(cancellationToken);

        var recipientIds = await _context.Users
            .Where(u => u.IsActive
                && u.Id != userId
                && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin || u.Role == UserRole.ProjectManager))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await _notifications.NotifyUsersAsync(
            recipientIds,
            NotificationType.ArticleOrderRequested,
            dto.Urgent ? "Urgent request for articles" : "Request for articles",
            $"{dto.RequestedByName} asked for {dto.Items.Count} article(s).",
            new Dictionary<string, string>
            {
                ["articleOrderId"] = dto.Id.ToString(),
                ["requestedByName"] = dto.RequestedByName,
                ["itemCount"] = dto.Items.Count.ToString(),
                ["urgent"] = dto.Urgent ? "true" : "false"
            },
            cancellationToken: cancellationToken);

        return dto;
    }
}
