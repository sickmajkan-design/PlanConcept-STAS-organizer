using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequest;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests.Commands.SubmitDocumentRequest;

/// <summary>One file of an answer.</summary>
public record SubmittedFile(string FileName, long SizeBytes, Stream Content);

/// <summary>
/// The person answers a request from their phone: one document, as one or several photographs or
/// pages.
/// </summary>
/// <remarks>
/// This is the only way a worker puts a file on their own record. It is deliberately narrower than a
/// general upload: it works only for a request addressed to the caller, only while the request is
/// waiting for an answer, takes the category from the request rather than from the client, and
/// accepts photographs and PDFs only. Nothing is shared with the person yet — that happens when the
/// office accepts it.
/// </remarks>
public record SubmitDocumentRequestCommand : IRequest<DocumentRequestDto>
{
    public Guid Id { get; init; }

    public List<SubmittedFile> Files { get; init; } = [];
}

public class SubmitDocumentRequestCommandValidator : AbstractValidator<SubmitDocumentRequestCommand>
{
    public SubmitDocumentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Files)
            .NotEmpty().WithMessage("Add at least one file.")
            .Must(f => f.Count <= DocumentRequestRules.MaxFilesPerSubmission)
            .WithMessage($"At most {DocumentRequestRules.MaxFilesPerSubmission} files per document.");

        RuleForEach(x => x.Files).ChildRules(file =>
        {
            file.RuleFor(f => f.SizeBytes)
                .GreaterThan(0).WithMessage("A file is empty.")
                .LessThanOrEqualTo(AttachmentRules.MaxSizeBytes)
                .WithMessage($"A file is larger than {AttachmentRules.MaxSizeBytes / (1024 * 1024)} MB.");
        });
    }
}

public class SubmitDocumentRequestCommandHandler : IRequestHandler<SubmitDocumentRequestCommand, DocumentRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _storage;
    private readonly INotificationService _notifications;
    private readonly IVirusScanner _virusScanner;

    public SubmitDocumentRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage storage,
        INotificationService notifications,
        IVirusScanner virusScanner)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storage = storage;
        _notifications = notifications;
        _virusScanner = virusScanner;
    }

    public async Task<DocumentRequestDto> Handle(
        SubmitDocumentRequestCommand command,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("Sign in to send a document.");

        // Only the person it is asked of. Anything else — someone else's, or none — is not found.
        var employeeId = _currentUserService.EmployeeId;

        var request = await _context.DocumentRequests
            .FirstOrDefaultAsync(
                r => r.Id == command.Id && employeeId != null && r.EmployeeId == employeeId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(DocumentRequest), command.Id);

        if (!DocumentRequestRules.CanBeAnswered(request.Status))
        {
            throw new ConflictException(
                request.Status == DocumentRequestStatus.Cancelled
                    ? "This request was withdrawn."
                    : "This request has already been answered. Reload it and try again.");
        }

        if (request.Attempts >= DocumentRequestRules.MaxAttempts)
        {
            throw new ConflictException(
                "Too many attempts for this request. Please speak to the office.");
        }

        // Check everything before storing anything, so a bad third file does not leave two behind.
        var prepared = new List<(Attachment Attachment, Stream Content, string ContentType)>();

        try
        {
            foreach (var file in command.Files)
            {
                var fileName = AttachmentRules.SanitiseFileName(file.FileName);
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                if (!DocumentRequestRules.SubmittableExtensions.Contains(extension))
                {
                    throw new ConflictException("Send a photograph or a PDF.");
                }

                var contentType = AttachmentRules.ResolveContentType(fileName)
                    ?? throw new ConflictException("That file type is not accepted.");

                var content = await UploadAttachmentCommandHandler.VerifySignatureAsync(
                    file.Content, fileName, cancellationToken);

                await _virusScanner.EnsureCleanAsync(content, cancellationToken);

                var attachment = new Attachment
                {
                    Id = Guid.NewGuid(),
                    FileName = fileName,
                    ContentType = contentType,
                    SizeBytes = file.SizeBytes,
                    Category = request.Category,
                    Title = request.Title,
                    UploadedByUserId = userId,
                    SharedWithEmployee = false
                };

                attachment.StorageKey = AttachmentRules.BuildStorageKey(
                    AttachmentOwnerType.Employee, request.EmployeeId, attachment.Id, fileName);

                AttachmentOwner.Apply(attachment, AttachmentOwnerType.Employee, request.EmployeeId);

                prepared.Add((attachment, content, contentType));
            }

            // Bytes first, rows second, as for any upload.
            var stored = new List<string>();

            try
            {
                foreach (var (attachment, content, contentType) in prepared)
                {
                    await _storage.SaveAsync(attachment.StorageKey, content, contentType, cancellationToken);
                    stored.Add(attachment.StorageKey);
                }

                request.Status = DocumentRequestStatus.Submitted;
                request.Attempts++;
                request.ReturnReason = null;

                _context.DocumentRequestEvents.Add(new DocumentRequestEvent
                {
                    Id = Guid.NewGuid(),
                    DocumentRequestId = request.Id,
                    Type = DocumentRequestEventType.Submitted,
                    ActorUserId = userId,
                    AttachmentIds = prepared.Select(p => p.Attachment.Id).ToArray()
                });

                _context.Attachments.AddRange(prepared.Select(p => p.Attachment));

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new ConflictException(
                        "This request was changed by someone else just now. Reload it and try again.");
                }
            }
            catch
            {
                foreach (var key in stored)
                {
                    await _storage.DeleteAsync(key, CancellationToken.None);
                }

                throw;
            }
        }
        finally
        {
            foreach (var (_, content, _) in prepared)
            {
                if (!ReferenceEquals(content, null) && content is MemoryStream)
                {
                    await content.DisposeAsync();
                }
            }
        }

        var recipients = await DocumentRequestRules.OfficeRecipientsAsync(_context, request, cancellationToken);

        await _notifications.NotifyUsersAsync(
            recipients,
            NotificationType.DocumentSubmitted,
            "Document received",
            "A document was sent and is waiting to be checked.",
            new Dictionary<string, string> { ["requestId"] = request.Id.ToString() },
            cancellationToken: cancellationToken);

        return await DocumentRequestDetail.LoadAsync(_context, _currentUserService, request.Id, cancellationToken);
    }
}
