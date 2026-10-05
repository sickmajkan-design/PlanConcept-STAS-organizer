using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Queries.GetAttachmentCounts;

/// <summary>How many documents one record has, and how many of them have lapsed.</summary>
public class AttachmentCountDto
{
    public Guid OwnerId { get; init; }

    public int Count { get; init; }

    public int Expired { get; init; }
}

/// <summary>
/// The document count of every record of one kind, in a single query — what a list page shows
/// beside each row without asking once per row. Records with no documents are simply absent.
/// </summary>
public record GetAttachmentCountsQuery : IRequest<IReadOnlyList<AttachmentCountDto>>
{
    public AttachmentOwnerType OwnerType { get; init; }
}

public class GetAttachmentCountsQueryHandler
    : IRequestHandler<GetAttachmentCountsQuery, IReadOnlyList<AttachmentCountDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAttachmentCountsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<AttachmentCountDto>> Handle(
        GetAttachmentCountsQuery request,
        CancellationToken cancellationToken)
    {
        // The same door as reading one record's documents: whoever may not see them may not
        // count them either.
        if (!AttachmentRules.CanRead(_currentUserService.Role, request.OwnerType))
        {
            throw new ForbiddenAccessException("You may not view the files on these records.");
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var query = _context.Attachments.AsNoTracking();

        // Exhaustive on purpose, like the single-record query: an owner kind the list pages do
        // not show is refused rather than answered with somebody else's numbers.
        var owned = request.OwnerType switch
        {
            AttachmentOwnerType.Employee => query.Where(a => a.EmployeeId != null)
                .Select(a => new { Owner = a.EmployeeId!.Value, a.ExpiresAt }),
            AttachmentOwnerType.Project => query.Where(a => a.ProjectId != null)
                .Select(a => new { Owner = a.ProjectId!.Value, a.ExpiresAt }),
            AttachmentOwnerType.Vehicle => query.Where(a => a.VehicleId != null)
                .Select(a => new { Owner = a.VehicleId!.Value, a.ExpiresAt }),
            AttachmentOwnerType.Tool => query.Where(a => a.ToolId != null)
                .Select(a => new { Owner = a.ToolId!.Value, a.ExpiresAt }),
            AttachmentOwnerType.Accommodation => query.Where(a => a.AccommodationId != null)
                .Select(a => new { Owner = a.AccommodationId!.Value, a.ExpiresAt }),
            _ => throw new ConflictException("Counts are available for employees, projects, vehicles, tools and housing.")
        };

        return await owned
            .GroupBy(a => a.Owner)
            .Select(g => new AttachmentCountDto
            {
                OwnerId = g.Key,
                Count = g.Count(),
                Expired = g.Count(a => a.ExpiresAt != null && a.ExpiresAt < today)
            })
            .ToListAsync(cancellationToken);
    }
}
