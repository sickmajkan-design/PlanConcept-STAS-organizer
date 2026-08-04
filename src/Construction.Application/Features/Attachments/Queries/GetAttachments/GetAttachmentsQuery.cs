using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Queries.GetAttachments;

/// <summary>Files attached to one record.</summary>
/// <remarks>
/// Not paged. A record's documents are counted in single digits, and paging a
/// list that short costs a second request to learn there is no second page.
/// </remarks>
public record GetAttachmentsQuery : IRequest<IReadOnlyList<AttachmentDto>>
{
    public AttachmentOwnerType OwnerType { get; init; }

    public Guid OwnerId { get; init; }

    public AttachmentCategory? Category { get; init; }
}

public class GetAttachmentsQueryValidator : AbstractValidator<GetAttachmentsQuery>
{
    public GetAttachmentsQueryValidator()
    {
        RuleFor(x => x.OwnerType).IsInEnum();
        RuleFor(x => x.OwnerId).NotEmpty().WithMessage("An owner record is required.");
    }
}

public class GetAttachmentsQueryHandler
    : IRequestHandler<GetAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetAttachmentsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AttachmentDto>> Handle(
        GetAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        var isOwnRecord = request.OwnerType == AttachmentOwnerType.Employee
            && _currentUserService.EmployeeId == request.OwnerId;

        // A worker who may photograph their own work item must be able to see
        // the photograph afterwards, or the upload is a write-only hole.
        var isOwnWorkItem = request.OwnerType == AttachmentOwnerType.WorkItem
            && await IsOwnWorkItemAsync(request.OwnerId, cancellationToken);

        // An employee always reaches their own file; everyone else needs the
        // role for this kind of owner.
        if (!isOwnRecord
            && !isOwnWorkItem
            && !AttachmentRules.CanRead(_currentUserService.Role, request.OwnerType))
        {
            throw new ForbiddenAccessException(
                "You may not view the files on this record.");
        }

        var query = _context.Attachments.AsNoTracking();

        // Exhaustive on purpose. This used to end in a `_` catch-all pointing
        // at ToolId, so an owner type the switch had not been taught returned
        // somebody else's files rather than none — a wrong answer instead of
        // an error, which is the harder kind to notice.
        query = request.OwnerType switch
        {
            AttachmentOwnerType.Employee => query.Where(a => a.EmployeeId == request.OwnerId),
            AttachmentOwnerType.Project => query.Where(a => a.ProjectId == request.OwnerId),
            AttachmentOwnerType.Vehicle => query.Where(a => a.VehicleId == request.OwnerId),
            AttachmentOwnerType.Tool => query.Where(a => a.ToolId == request.OwnerId),
            AttachmentOwnerType.WorkItem => query.Where(a => a.WorkItemId == request.OwnerId),
            _ => throw new ArgumentOutOfRangeException(
                nameof(request),
                request.OwnerType,
                "Unknown attachment owner type.")
        };

        if (request.Category is { } category)
        {
            query = query.Where(a => a.Category == category);
        }

        return await query
            // Anything with a deadline first, soonest at the top, then the
            // rest newest-first. That puts a lapsed certificate where it is
            // seen rather than wherever its upload date happens to land.
            .OrderBy(a => a.ExpiresAt == null)
            .ThenBy(a => a.ExpiresAt)
            .ThenByDescending(a => a.CreatedAt)
            .ProjectTo<AttachmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// True when the caller is a worker asking about their own work item.
    /// </summary>
    /// <remarks>
    /// Only consulted for workers: everyone above them already passes
    /// <see cref="AttachmentRules.CanRead"/> and this would be a needless
    /// query. A missing item answers false, so a guessed id is refused by the
    /// role check rather than by a lookup that would confirm it exists.
    /// </remarks>
    private async Task<bool> IsOwnWorkItemAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role is not UserRole.Worker)
        {
            return false;
        }

        var owner = await _context.WorkItems
            .AsNoTracking()
            .Where(w => w.Id == id)
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
