using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Queries.GetAttachmentContent;

/// <summary>The bytes of one attachment, ready to stream to the caller.</summary>
public record AttachmentContent(Stream Content, string ContentType, string FileName);

public record GetAttachmentContentQuery(Guid Id) : IRequest<AttachmentContent>;

public class GetAttachmentContentQueryHandler
    : IRequestHandler<GetAttachmentContentQuery, AttachmentContent>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;

    public GetAttachmentContentQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
    }

    public async Task<AttachmentContent> Handle(
        GetAttachmentContentQuery request,
        CancellationToken cancellationToken)
    {
        var attachment = await _context.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), request.Id);

        var (ownerType, _) = AttachmentOwner.Of(attachment);

        var allowed =
            AttachmentRules.CanRead(_currentUserService.Role, ownerType)
            || AttachmentRules.CanReadOwnEmployeeFiles(
                _currentUserService.EmployeeId, attachment);

        if (!allowed)
        {
            // 404 rather than 403: the id is a guess either way, and a 403
            // would confirm the guess landed on a real document.
            throw new NotFoundException(nameof(Attachment), request.Id);
        }

        var content = await _storage.OpenReadAsync(attachment.StorageKey, cancellationToken)
            ?? throw new NotFoundException(
                "The file is recorded but its contents are missing from storage.");

        return new AttachmentContent(content, attachment.ContentType, attachment.FileName);
    }
}
