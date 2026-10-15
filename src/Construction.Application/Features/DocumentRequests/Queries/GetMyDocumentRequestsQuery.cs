using Construction.Application.Common.Interfaces;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests.Queries.GetMyDocumentRequests;

/// <summary>What the office has asked the caller for, and where each stands.</summary>
/// <remarks>
/// Identity comes from the token. An account with no employee record has nothing asked of it.
/// Withdrawn requests stay visible for a month so the person is not left wondering where one went.
/// </remarks>
public record GetMyDocumentRequestsQuery : IRequest<IReadOnlyList<DocumentRequestDto>>;

public class GetMyDocumentRequestsQueryHandler
    : IRequestHandler<GetMyDocumentRequestsQuery, IReadOnlyList<DocumentRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetMyDocumentRequestsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<DocumentRequestDto>> Handle(
        GetMyDocumentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.EmployeeId is not { } employeeId)
        {
            return [];
        }

        var cutoff = _dateTimeProvider.UtcNow.AddDays(-30);

        var items = await _context.DocumentRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId
                && (r.Status != DocumentRequestStatus.Cancelled || r.UpdatedAt >= cutoff))
            .OrderBy(r => r.Status == DocumentRequestStatus.Returned ? 0
                : r.Status == DocumentRequestStatus.Open ? 1
                : r.Status == DocumentRequestStatus.Submitted ? 2 : 3)
            .ThenBy(r => r.DueDate == null)
            .ThenBy(r => r.DueDate)
            .ThenByDescending(r => r.CreatedAt)
            .Select(DocumentRequestMapping.Projection)
            .ToListAsync(cancellationToken);

        // Who asked is not the person's business.
        return items.Select(i => i with { RequestedByName = null }).ToList();
    }
}
