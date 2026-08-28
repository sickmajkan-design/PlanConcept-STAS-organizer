using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Bulletin.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Bulletin.Queries.GetBulletinPosts;

/// <summary>Every bulletin currently posted, newest first.</summary>
/// <remarks>
/// Not paged — a board with fifty live notices at once has stopped being a
/// board an admin ever meant to build; paging it would treat clutter as a
/// feature instead of a sign to go remove some.
/// </remarks>
public record GetBulletinPostsQuery : IRequest<IReadOnlyList<BulletinPostDto>>;

public class GetBulletinPostsQueryHandler
    : IRequestHandler<GetBulletinPostsQuery, IReadOnlyList<BulletinPostDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBulletinPostsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<BulletinPostDto>> Handle(
        GetBulletinPostsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        return await _context.BulletinPosts
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(BulletinPostMapping.ProjectionFor(userId))
            .ToListAsync(cancellationToken);
    }
}
