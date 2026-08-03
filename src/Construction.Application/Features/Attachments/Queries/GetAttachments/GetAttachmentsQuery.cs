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

        // An employee always reaches their own file; everyone else needs the
        // role for this kind of owner.
        if (!isOwnRecord
            && !AttachmentRules.CanRead(_currentUserService.Role, request.OwnerType))
        {
            throw new ForbiddenAccessException(
                "You may not view the files on this record.");
        }

        var query = _context.Attachments.AsNoTracking();

        query = request.OwnerType switch
        {
            AttachmentOwnerType.Employee => query.Where(a => a.EmployeeId == request.OwnerId),
            AttachmentOwnerType.Project => query.Where(a => a.ProjectId == request.OwnerId),
            AttachmentOwnerType.Vehicle => query.Where(a => a.VehicleId == request.OwnerId),
            _ => query.Where(a => a.ToolId == request.OwnerId)
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
}
