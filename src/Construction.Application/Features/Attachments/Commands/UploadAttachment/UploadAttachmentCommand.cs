using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Commands.UploadAttachment;

/// <summary>Attaches a file to one record.</summary>
public record UploadAttachmentCommand : IRequest<AttachmentDto>
{
    public AttachmentOwnerType OwnerType { get; init; }

    public Guid OwnerId { get; init; }

    public AttachmentCategory Category { get; init; } = AttachmentCategory.Other;

    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    /// <summary>The bytes. Never buffered in full — copied straight to storage.</summary>
    public Stream Content { get; init; } = Stream.Null;

    public string? Description { get; init; }

    public DateOnly? ExpiresAt { get; init; }
}

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.OwnerType).IsInEnum();

        RuleFor(x => x.OwnerId).NotEmpty().WithMessage("An owner record is required.");

        RuleFor(x => x.Category).IsInEnum();

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("A file name is required.")
            .MaximumLength(512);

        RuleFor(x => x.FileName)
            .Must(name => AttachmentRules.ResolveContentType(name) is not null)
            .WithMessage(
                "That file type is not accepted. Allowed: " +
                string.Join(", ", AttachmentRules.AllowedTypesByExtension.Keys))
            .When(x => !string.IsNullOrWhiteSpace(x.FileName));

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(AttachmentRules.MaxSizeBytes)
            .WithMessage(
                $"The file is larger than the {AttachmentRules.MaxSizeBytes / (1024 * 1024)} MB limit.");

        RuleFor(x => x.Description).MaximumLength(1000);

        // A photograph does not lapse, and an expiry on one would put it in
        // the reminder sweep for no reason.
        RuleFor(x => x.ExpiresAt)
            .Null().WithMessage("A photograph does not expire.")
            .When(x => x.Category == AttachmentCategory.Photo);
    }
}

public class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;

    public UploadAttachmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
    }

    public async Task<AttachmentDto> Handle(
        UploadAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (!AttachmentRules.CanUpload(
                _currentUserService.Role, request.OwnerType, request.Category))
        {
            throw new ForbiddenAccessException(
                "You may not attach files of this kind to this record.");
        }

        await EnsureOwnerExistsAsync(request.OwnerType, request.OwnerId, cancellationToken);

        await EnsureWorkItemIsTheirsAsync(request, cancellationToken);

        var fileName = AttachmentRules.SanitiseFileName(request.FileName);

        // Re-derived from the name rather than trusted from the upload: the
        // client sets its own Content-Type, and this value later tells a
        // browser whether to render the file.
        var contentType = AttachmentRules.ResolveContentType(fileName)
            ?? throw new ConflictException("That file type is not accepted.");

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = request.SizeBytes,
            Category = request.Category,
            Description = request.Description?.Trim(),
            ExpiresAt = request.ExpiresAt,
            UploadedByUserId = _currentUserService.UserId
        };

        attachment.StorageKey = AttachmentRules.BuildStorageKey(
            request.OwnerType, request.OwnerId, attachment.Id, fileName);

        AttachmentOwner.Apply(attachment, request.OwnerType, request.OwnerId);

        // Bytes first, row second. The other order can leave a row pointing at
        // nothing if the write fails, and a row that promises a file it cannot
        // produce is worse than an orphaned object nobody can reach: the
        // object costs storage, the row costs someone their evidence.
        await _storage.SaveAsync(
            attachment.StorageKey, request.Content, contentType, cancellationToken);

        try
        {
            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Do not leave the object behind when the row could not be saved.
            await _storage.DeleteAsync(attachment.StorageKey, CancellationToken.None);
            throw;
        }

        return await _context.Attachments
            .AsNoTracking()
            .Where(a => a.Id == attachment.Id)
            .Select(AttachmentMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// Keeps a worker's work-item photo on a work item that is theirs.
    /// </summary>
    /// <remarks>
    /// Runs after the existence check, so an item that does not exist answers
    /// 404 rather than 403 — a 403 here would confirm that a guessed id is
    /// real, which is the same reasoning the read paths use.
    /// </remarks>
    private async Task EnsureWorkItemIsTheirsAsync(
        UploadAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.OwnerType != AttachmentOwnerType.WorkItem)
        {
            return;
        }

        var owner = await _context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == request.OwnerId)
            .Select(w => new { w.CreatedByUserId, w.AssignedEmployeeId })
            .FirstAsync(cancellationToken);

        if (!AttachmentRules.CanUploadToWorkItem(
                _currentUserService.Role,
                _currentUserService.UserId,
                _currentUserService.EmployeeId,
                owner.CreatedByUserId,
                owner.AssignedEmployeeId))
        {
            throw new ForbiddenAccessException(
                "You may only add photographs to your own work.");
        }
    }

    private async Task EnsureOwnerExistsAsync(
        AttachmentOwnerType type,
        Guid id,
        CancellationToken cancellationToken)
    {
        var exists = type switch
        {
            AttachmentOwnerType.Employee =>
                await _context.Employees.AnyAsync(e => e.Id == id, cancellationToken),
            AttachmentOwnerType.Project =>
                await _context.Projects.AnyAsync(p => p.Id == id, cancellationToken),
            AttachmentOwnerType.Vehicle =>
                await _context.Vehicles.AnyAsync(v => v.Id == id, cancellationToken),
            AttachmentOwnerType.Tool =>
                await _context.Tools.AnyAsync(t => t.Id == id, cancellationToken),
            AttachmentOwnerType.WorkItem =>
                await _context.WorkItems.AnyAsync(w => w.Id == id, cancellationToken),
            _ => false
        };

        if (!exists)
        {
            throw new NotFoundException(type.ToString(), id);
        }
    }
}
