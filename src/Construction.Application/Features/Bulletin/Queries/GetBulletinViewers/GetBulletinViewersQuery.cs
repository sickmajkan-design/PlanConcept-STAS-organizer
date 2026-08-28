using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Bulletin.Models;
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
        return await _context.BulletinViews
            .AsNoTracking()
            .Where(v => v.BulletinPostId == request.PostId)
            .OrderByDescending(v => v.ViewedAt)
            .Select(BulletinViewerMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
