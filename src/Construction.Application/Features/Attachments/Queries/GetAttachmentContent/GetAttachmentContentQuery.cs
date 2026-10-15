using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
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
                _currentUserService.EmployeeId, attachment)
            // The same carve-out the list query makes: a worker who may
            // photograph their own work item has to be able to open the
            // picture afterwards.
            || await IsOnAssignedVehicleAsync(attachment, cancellationToken)
            || await IsSentByCallerAsync(attachment, cancellationToken)
            || await IsOnOwnWorkItemAsync(attachment, cancellationToken)
            || await IsOnOwnRefundAsync(attachment, cancellationToken);

        if (allowed && ownerType == AttachmentOwnerType.BookEntry)
        {
            allowed = await Book.BookRules.HasAccessAsync(_context, _currentUserService, cancellationToken);
        }

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

    /// <summary>
    /// True when this file is one the caller sent in answer to a request of theirs — they may open
    /// what they sent while it waits to be checked, though it is not shared with them yet.
    /// </summary>
    private async Task<bool> IsSentByCallerAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        if (_currentUserService.EmployeeId is not { } employeeId || attachment.EmployeeId != employeeId)
        {
            return false;
        }

        return await _context.DocumentRequestEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.Type == DocumentRequestEventType.Submitted
                    && e.AttachmentIds.Contains(attachment.Id)
                    && e.DocumentRequest.EmployeeId == employeeId,
                cancellationToken);
    }

    /// <summary>True when this shared file hangs off the vehicle the caller is currently assigned.</summary>
    private async Task<bool> IsOnAssignedVehicleAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        if (attachment.VehicleId is not { } vehicleId || !attachment.SharedWithEmployee)
        {
            return false;
        }

        var driver = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.Id == vehicleId)
            .Select(v => v.AssignedEmployeeId)
            .FirstOrDefaultAsync(cancellationToken);

        return AttachmentRules.CanReadAssignedVehicleFiles(
            _currentUserService.EmployeeId, attachment, driver);
    }

    /// <summary>True when this receipt hangs off a refund the caller asked for.</summary>
    private async Task<bool> IsOnOwnRefundAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        if (attachment.RefundId is not { } refundId)
        {
            return false;
        }

        var requestedBy = await _context.Refunds
            .AsNoTracking()
            .Where(r => r.Id == refundId)
            .Select(r => (Guid?)r.RequestedByUserId)
            .FirstOrDefaultAsync(cancellationToken);

        return requestedBy is not null
            && AttachmentRules.CanUseRefundFiles(_currentUserService.Role, _currentUserService.UserId, requestedBy.Value);
    }

    /// <summary>
    /// True when this file hangs off a work item the caller raised or is
    /// assigned to. Only consulted for workers; everyone above them is already
    /// allowed by role.
    /// </summary>
    private async Task<bool> IsOnOwnWorkItemAsync(
        Attachment attachment,
        CancellationToken cancellationToken)
    {
        if (attachment.WorkItemId is not { } workItemId
            || _currentUserService.Role is not UserRole.Worker)
        {
            return false;
        }

        var owner = await _context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == workItemId)
            .Select(w => new { w.CreatedByUserId, w.AssignedEmployeeId })
            .FirstOrDefaultAsync(cancellationToken);

        return owner is not null
            && AttachmentRules.CanUploadToWorkItem(
                UserRole.Worker,
                _currentUserService.UserId,
                _currentUserService.EmployeeId,
                owner.CreatedByUserId,
                owner.AssignedEmployeeId);
    }
}
