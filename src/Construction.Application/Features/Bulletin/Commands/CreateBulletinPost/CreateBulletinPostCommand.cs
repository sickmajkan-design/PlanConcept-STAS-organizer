using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Bulletin.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Bulletin.Commands.CreateBulletinPost;

/// <summary>Pins a new notice to the board.</summary>
public record CreateBulletinPostCommand : IRequest<BulletinPostDto>
{
    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;
}

public class CreateBulletinPostCommandValidator : AbstractValidator<CreateBulletinPostCommand>
{
    public CreateBulletinPostCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}

public class CreateBulletinPostCommandHandler
    : IRequestHandler<CreateBulletinPostCommand, BulletinPostDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public CreateBulletinPostCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task<BulletinPostDto> Handle(
        CreateBulletinPostCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var post = new BulletinPost
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            CreatedByUserId = userId,
        };

        _context.BulletinPosts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        // Everyone signed in can already see the board — this is a nudge to
        // open it, not the only way to find out. Never blocking: the board
        // was deliberately built as "come look" rather than "acknowledge
        // this before you can do anything else", and that stays true here.
        var recipientIds = await _context.Users
            .Where(u => u.IsActive && u.Id != userId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (recipientIds.Count > 0)
        {
            await _notifications.NotifyUsersAsync(
                recipientIds,
                NotificationType.BulletinPosted,
                "New notice on the bulletin board",
                post.Title,
                data: new Dictionary<string, string> { ["bulletinPostId"] = post.Id.ToString() },
                requiresAcknowledgment: false,
                cancellationToken: cancellationToken);
        }

        return new BulletinPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            CreatedByName = _currentUserService.Email ?? "",
            CreatedAt = post.CreatedAt,
            ViewCount = 0,
            Viewed = false,
        };
    }
}
