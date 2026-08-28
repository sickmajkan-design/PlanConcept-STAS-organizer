using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Bulletin.Commands.MarkBulletinViewed;

/// <summary>
/// Records that the caller has seen a bulletin post. Idempotent — a second
/// view of the same post by the same person changes nothing, since the
/// roster only ever needed the first.
/// </summary>
public record MarkBulletinViewedCommand(Guid PostId) : IRequest;

public class MarkBulletinViewedCommandHandler : IRequestHandler<MarkBulletinViewedCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkBulletinViewedCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(MarkBulletinViewedCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var postExists = await _context.BulletinPosts
            .AnyAsync(p => p.Id == request.PostId, cancellationToken);

        if (!postExists)
        {
            throw new NotFoundException(nameof(BulletinPost), request.PostId);
        }

        var alreadyViewed = await _context.BulletinViews
            .AnyAsync(
                v => v.BulletinPostId == request.PostId && v.UserId == userId,
                cancellationToken);

        if (alreadyViewed)
        {
            return;
        }

        _context.BulletinViews.Add(new BulletinView
        {
            BulletinPostId = request.PostId,
            UserId = userId,
            ViewedAt = _dateTimeProvider.UtcNow,
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two requests racing to record the same first view: the unique
            // index on (PostId, UserId) refuses the loser, and the loser is
            // told the same thing a winner would report — the view is
            // recorded, which it now is either way.
        }
    }
}
