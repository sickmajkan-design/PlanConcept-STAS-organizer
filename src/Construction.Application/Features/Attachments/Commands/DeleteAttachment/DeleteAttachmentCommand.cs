using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Commands.DeleteAttachment;

/// <summary>Removes an attachment and the bytes behind it.</summary>
/// <remarks>
/// The row is soft-deleted like everything else here, but the stored object is
/// removed outright, so this is not reversible. That asymmetry is deliberate:
/// the row is kept as a trace that a file existed and who removed it, while
/// the file itself has to actually go — a deletion request for someone's
/// medical record is not satisfied by hiding it from a list.
/// </remarks>
public record DeleteAttachmentCommand(Guid Id) : IRequest;

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;

    public DeleteAttachmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
    }

    public async Task Handle(
        DeleteAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (!AttachmentRules.CanDelete(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not delete attachments.");
        }

        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), request.Id);

        var storageKey = attachment.StorageKey;

        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        // Row first, bytes second — the reverse of upload, and for the same
        // reason. If this throws, the listing already no longer offers the
        // file and an unreachable object is left behind; the alternative is a
        // row still offering a file that is already gone.
        await _storage.DeleteAsync(storageKey, cancellationToken);
    }
}
