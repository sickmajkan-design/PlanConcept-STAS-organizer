using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Commands.UpdateAttachment;

/// <summary>
/// Edits a document's details — type, note, expiry and retention — without
/// touching the file itself.
/// </summary>
/// <remarks>
/// This is how a lapsing certificate is renewed in place: the new validity date
/// is set here, and the expiry reminders and the nav badge follow. The file, its
/// name and its owner stay as they are; replacing the bytes is an upload.
/// Admin and above, the same as deleting.
/// </remarks>
public record UpdateAttachmentCommand : IRequest<AttachmentDto>
{
    public Guid Id { get; init; }

    public AttachmentCategory Category { get; init; }

    public string? Description { get; init; }

    public DateOnly? ExpiresAt { get; init; }

    public DateOnly? RetainUntil { get; init; }
}

public class UpdateAttachmentCommandValidator : AbstractValidator<UpdateAttachmentCommand>
{
    public UpdateAttachmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.ExpiresAt)
            .Null().WithMessage("A photograph does not expire.")
            .When(x => x.Category == AttachmentCategory.Photo);
    }
}

public class UpdateAttachmentCommandHandler : IRequestHandler<UpdateAttachmentCommand, AttachmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateAttachmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AttachmentDto> Handle(
        UpdateAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (!AttachmentRules.CanDelete(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not edit attachments.");
        }

        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), request.Id);

        attachment.Category = request.Category;
        attachment.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        attachment.ExpiresAt = request.ExpiresAt;
        attachment.RetainUntil = request.RetainUntil;

        await _context.SaveChangesAsync(cancellationToken);

        return AttachmentMapping.ToDto(attachment);
    }
}
