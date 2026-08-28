using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Bulletin.Commands.DeleteBulletinPost;

/// <summary>Takes a notice down. Its view roster goes with it.</summary>
public record DeleteBulletinPostCommand(Guid Id) : IRequest;

public class DeleteBulletinPostCommandHandler : IRequestHandler<DeleteBulletinPostCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteBulletinPostCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteBulletinPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.BulletinPosts
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BulletinPost), request.Id);

        _context.BulletinPosts.Remove(post);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
