using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Commands.SetAttachmentSharing;

/// <summary>
/// Shares documents with the person they are about, or takes the sharing back.
/// </summary>
/// <remarks>
/// A document is shared with the employee it is filed against, or with whoever the vehicle it is
/// filed against is assigned to. Nothing is shared by default; this is the one switch. Admin and
/// above, the same as editing a document. All-or-nothing: if any id cannot be shared, none change.
/// </remarks>
public record SetAttachmentSharingCommand : IRequest<int>
{
    public List<Guid> Ids { get; init; } = [];

    public bool Shared { get; init; }
}

public class SetAttachmentSharingCommandValidator : AbstractValidator<SetAttachmentSharingCommand>
{
    public const int MaxBatch = 200;

    public SetAttachmentSharingCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("Choose at least one document.")
            .Must(ids => ids.Count <= MaxBatch)
            .WithMessage($"At most {MaxBatch} documents at a time.");
    }
}

public class SetAttachmentSharingCommandHandler : IRequestHandler<SetAttachmentSharingCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetAttachmentSharingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(
        SetAttachmentSharingCommand request,
        CancellationToken cancellationToken)
    {
        if (!AttachmentRules.CanDelete(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not share documents.");
        }

        var ids = request.Ids.Distinct().ToList();

        var attachments = await _context.Attachments
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(cancellationToken);

        if (attachments.Count != ids.Count)
        {
            // Unknown, deleted or out-of-scope ids look the same, and nothing is changed.
            throw new NotFoundException(nameof(Attachment), ids.First(i => attachments.All(a => a.Id != i)));
        }

        foreach (var attachment in attachments)
        {
            var (ownerType, _) = AttachmentOwner.Of(attachment);

            if (!AttachmentRules.CanBeShared(ownerType))
            {
                throw new ConflictException(
                    "Only documents on an employee or a vehicle can be shared with a person.");
            }
        }

        foreach (var attachment in attachments)
        {
            attachment.SharedWithEmployee = request.Shared;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return attachments.Count;
    }
}
