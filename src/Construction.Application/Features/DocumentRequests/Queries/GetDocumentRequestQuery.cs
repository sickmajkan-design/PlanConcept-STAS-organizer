using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequest;

/// <summary>One request with its history, for the office or for the person it is about.</summary>
public record GetDocumentRequestQuery(Guid Id) : IRequest<DocumentRequestDto>;

public class GetDocumentRequestQueryHandler : IRequestHandler<GetDocumentRequestQuery, DocumentRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentRequestQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public Task<DocumentRequestDto> Handle(GetDocumentRequestQuery request, CancellationToken cancellationToken) =>
        DocumentRequestDetail.LoadAsync(_context, _currentUserService, request.Id, cancellationToken);
}

/// <summary>Builds the full picture of one request, enforcing who may see it.</summary>
public static class DocumentRequestDetail
{
    /// <summary>
    /// Loads a request the caller may see: the office sees any, a person only their own. Anything else
    /// is not found — a guessed id learns nothing.
    /// </summary>
    public static async Task<DocumentRequestDto> LoadAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        Guid id,
        CancellationToken cancellationToken)
    {
        var office = DocumentRequestRules.CanManage(currentUser.Role);

        var query = context.DocumentRequests.AsNoTracking().Where(r => r.Id == id);

        if (!office)
        {
            var self = currentUser.EmployeeId;
            query = query.Where(r => self != null && r.EmployeeId == self);
        }

        var dto = await query
            .Select(DocumentRequestMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(DocumentRequest), id);

        var events = await context.DocumentRequestEvents
            .AsNoTracking()
            .Where(e => e.DocumentRequestId == id)
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        var attachmentIds = events.SelectMany(e => e.AttachmentIds).Distinct().ToList();

        var files = attachmentIds.Count == 0
            ? new Dictionary<Guid, DocumentRequestFileDto>()
            : await context.Attachments
                .AsNoTracking()
                .Where(a => attachmentIds.Contains(a.Id))
                .Select(a => new DocumentRequestFileDto(a.Id, a.FileName, a.ContentType, a.SizeBytes))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

        IReadOnlyList<DocumentRequestFileDto> FilesOf(IEnumerable<Guid> ids) =>
            ids.Where(files.ContainsKey).Select(i => files[i]).ToList();

        var actorIds = events.Where(e => e.ActorUserId != null).Select(e => e.ActorUserId!.Value).Distinct().ToList();
        var actors = office && actorIds.Count > 0
            ? await context.Users
                .AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken)
            : new Dictionary<Guid, string>();

        var employeeUserIds = await context.Users
            .AsNoTracking()
            .Where(u => u.EmployeeId == dto.EmployeeId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var latestSubmission = events.LastOrDefault(e => e.Type == Domain.Enums.DocumentRequestEventType.Submitted);

        return dto with
        {
            // Who asked is the office's business: the person is only told it was the office.
            RequestedByName = office ? dto.RequestedByName : null,
            Files = latestSubmission is null ? [] : FilesOf(latestSubmission.AttachmentIds),
            Events = events
                .Select(e => new DocumentRequestEventDto(
                    e.Type,
                    e.CreatedAt,
                    e.ActorUserId is { } actor && employeeUserIds.Contains(actor),
                    office && e.ActorUserId is { } a && actors.TryGetValue(a, out var name) ? name : null,
                    e.Reason,
                    FilesOf(e.AttachmentIds)))
                .ToList()
        };
    }
}
