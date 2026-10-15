using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequest;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests.Commands.ReviewDocumentRequest;

/// <summary>Accepts the document that was sent: it goes onto the person's record and is shared with them.</summary>
public record AcceptDocumentRequestCommand : IRequest<DocumentRequestDto>
{
    public Guid Id { get; init; }

    /// <summary>The day the document stops being valid. Required for documents that lapse.</summary>
    public DateOnly? ExpiresAt { get; init; }
}

/// <summary>Sends the document back, with the reason, and asks for a new one.</summary>
public record ReturnDocumentRequestCommand : IRequest<DocumentRequestDto>
{
    public Guid Id { get; init; }

    public string Reason { get; init; } = null!;
}

/// <summary>Withdraws a request.</summary>
public record CancelDocumentRequestCommand(Guid Id) : IRequest<DocumentRequestDto>;

public class ReturnDocumentRequestCommandValidator : AbstractValidator<ReturnDocumentRequestCommand>
{
    public ReturnDocumentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Say why, so the person knows what to fix.")
            .Must(r => (r ?? string.Empty).Trim().Length >= DocumentRequestRules.MinReasonLength)
            .WithMessage($"The reason must be at least {DocumentRequestRules.MinReasonLength} characters.")
            .MaximumLength(DocumentRequestRules.MaxNoteLength);
    }
}

/// <summary>The three decisions the office takes on a request share their load, checks and notice.</summary>
public abstract class ReviewHandlerBase
{
    protected ReviewHandlerBase(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        Context = context;
        CurrentUser = currentUserService;
        Clock = dateTimeProvider;
        Notifications = notifications;
    }

    protected IApplicationDbContext Context { get; }

    protected ICurrentUserService CurrentUser { get; }

    protected IDateTimeProvider Clock { get; }

    protected INotificationService Notifications { get; }

    protected Guid RequireOffice()
    {
        if (!DocumentRequestRules.CanManage(CurrentUser.Role))
        {
            throw new ForbiddenAccessException("You may not decide on requests for documents.");
        }

        return CurrentUser.UserId ?? throw new ForbiddenAccessException("Sign in first.");
    }

    protected async Task<DocumentRequest> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await Context.DocumentRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
        ?? throw new NotFoundException(nameof(DocumentRequest), id);

    protected void Record(
        DocumentRequest request,
        DocumentRequestEventType type,
        Guid actor,
        string? reason = null) =>
        Context.DocumentRequestEvents.Add(new DocumentRequestEvent
        {
            Id = Guid.NewGuid(),
            DocumentRequestId = request.Id,
            Type = type,
            ActorUserId = actor,
            Reason = reason
        });

    protected async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This request was changed by someone else just now. Reload it and try again.");
        }
    }

    /// <summary>A generic notice to the person: the type of document and any reason stay inside the app.</summary>
    protected async Task TellPersonAsync(
        DocumentRequest request,
        NotificationType type,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var recipients = await DocumentRequestRules.UserIdsOfEmployeeAsync(
            Context, request.EmployeeId, cancellationToken);

        if (recipients.Count == 0)
        {
            return;
        }

        await Notifications.NotifyUsersAsync(
            recipients,
            type,
            title,
            body,
            new Dictionary<string, string> { ["requestId"] = request.Id.ToString() },
            cancellationToken: cancellationToken);
    }
}

public class AcceptDocumentRequestCommandHandler
    : ReviewHandlerBase, IRequestHandler<AcceptDocumentRequestCommand, DocumentRequestDto>
{
    public AcceptDocumentRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
        : base(context, currentUserService, dateTimeProvider, notifications)
    {
    }

    public async Task<DocumentRequestDto> Handle(
        AcceptDocumentRequestCommand command,
        CancellationToken cancellationToken)
    {
        var actor = RequireOffice();
        var request = await LoadAsync(command.Id, cancellationToken);

        if (request.Status != DocumentRequestStatus.Submitted)
        {
            throw new ConflictException("This request is not waiting to be checked. Reload it and try again.");
        }

        if (command.ExpiresAt is null && DocumentRequestRules.NeedsExpiry(request.Category))
        {
            throw new ConflictException("Enter the date this document is valid until.");
        }

        var latest = await Context.DocumentRequestEvents
            .Where(e => e.DocumentRequestId == request.Id && e.Type == DocumentRequestEventType.Submitted)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("Nothing was sent for this request.");

        var files = await Context.Attachments
            .Where(a => latest.AttachmentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        if (files.Count == 0)
        {
            // Removed from the record after it was sent: there is nothing to accept.
            throw new ConflictException("The files that were sent are no longer on the record. Ask for the document again.");
        }

        foreach (var file in files)
        {
            file.SharedWithEmployee = true;
            file.ExpiresAt = command.ExpiresAt;
        }

        request.Status = DocumentRequestStatus.Accepted;
        request.ReviewedByUserId = actor;
        request.ReviewedAt = Clock.UtcNow;
        request.ReturnReason = null;

        Record(request, DocumentRequestEventType.Accepted, actor);
        await SaveAsync(cancellationToken);

        await TellPersonAsync(
            request,
            NotificationType.DocumentAccepted,
            "Document accepted",
            "The office accepted your document.",
            cancellationToken);

        return await DocumentRequestDetail.LoadAsync(Context, CurrentUser, request.Id, cancellationToken);
    }
}

public class ReturnDocumentRequestCommandHandler
    : ReviewHandlerBase, IRequestHandler<ReturnDocumentRequestCommand, DocumentRequestDto>
{
    public ReturnDocumentRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
        : base(context, currentUserService, dateTimeProvider, notifications)
    {
    }

    public async Task<DocumentRequestDto> Handle(
        ReturnDocumentRequestCommand command,
        CancellationToken cancellationToken)
    {
        var actor = RequireOffice();
        var request = await LoadAsync(command.Id, cancellationToken);

        if (request.Status != DocumentRequestStatus.Submitted)
        {
            throw new ConflictException("This request is not waiting to be checked. Reload it and try again.");
        }

        var reason = command.Reason.Trim();

        request.Status = DocumentRequestStatus.Returned;
        request.ReviewedByUserId = actor;
        request.ReviewedAt = Clock.UtcNow;
        request.ReturnReason = reason;

        Record(request, DocumentRequestEventType.Returned, actor, reason);
        await SaveAsync(cancellationToken);

        // The reason is read inside the app, never on a lock screen.
        await TellPersonAsync(
            request,
            NotificationType.DocumentReturned,
            "Document returned",
            "The office needs a new document. Open the app to see why.",
            cancellationToken);

        return await DocumentRequestDetail.LoadAsync(Context, CurrentUser, request.Id, cancellationToken);
    }
}

public class CancelDocumentRequestCommandHandler
    : ReviewHandlerBase, IRequestHandler<CancelDocumentRequestCommand, DocumentRequestDto>
{
    public CancelDocumentRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
        : base(context, currentUserService, dateTimeProvider, notifications)
    {
    }

    public async Task<DocumentRequestDto> Handle(
        CancelDocumentRequestCommand command,
        CancellationToken cancellationToken)
    {
        var actor = RequireOffice();
        var request = await LoadAsync(command.Id, cancellationToken);

        if (request.Status is DocumentRequestStatus.Accepted or DocumentRequestStatus.Cancelled)
        {
            throw new ConflictException("This request is already closed. Reload it and try again.");
        }

        request.Status = DocumentRequestStatus.Cancelled;
        request.ReviewedByUserId = actor;
        request.ReviewedAt = Clock.UtcNow;

        Record(request, DocumentRequestEventType.Cancelled, actor);
        await SaveAsync(cancellationToken);

        await TellPersonAsync(
            request,
            NotificationType.DocumentRequestCancelled,
            "Request withdrawn",
            "The office withdrew a request. You do not need to do anything.",
            cancellationToken);

        return await DocumentRequestDetail.LoadAsync(Context, CurrentUser, request.Id, cancellationToken);
    }
}
