using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Bulletin.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Bulletin.Queries.GetBulletinViewers;

/// <summary>Who has seen one bulletin post, and when — the admin-facing roster.</summary>
public record GetBulletinViewersQuery(Guid PostId) : IRequest<IReadOnlyList<BulletinViewerDto>>;

public class GetBulletinViewersQueryHandler
    : IRequestHandler<GetBulletinViewersQuery, IReadOnlyList<BulletinViewerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBulletinViewersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BulletinViewerDto>> Handle(
        GetBulletinViewersQuery request,
        CancellationToken cancellationToken)
    {
        var postExists = await _context.BulletinPosts
            .AnyAsync(p => p.Id == request.PostId, cancellationToken);

        if (!postExists)
        {
            throw new NotFoundException(nameof(BulletinPost), request.PostId);
        }

        return await _context.BulletinViews
            .AsNoTracking()
            .Where(v => v.BulletinPostId == request.PostId)
            .OrderByDescending(v => v.ViewedAt)
            .Select(BulletinViewerMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
