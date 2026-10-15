using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.DocumentRequests.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests.Queries.GetDocumentRequests;

/// <summary>The office's list of requests: by status, by person, or only those past their date.</summary>
public record GetDocumentRequestsQuery : IPagedQuery, IRequest<PagedList<DocumentRequestDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public DocumentRequestStatus? Status { get; init; }

    public Guid? EmployeeId { get; init; }

    /// <summary>Only requests still waiting on the person whose date has passed.</summary>
    public bool Overdue { get; init; }
}

public class GetDocumentRequestsQueryValidator : PagedQueryValidator<GetDocumentRequestsQuery>;

public class GetDocumentRequestsQueryHandler
    : IRequestHandler<GetDocumentRequestsQuery, PagedList<DocumentRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetDocumentRequestsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedList<DocumentRequestDto>> Handle(
        GetDocumentRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (!DocumentRequestRules.CanManage(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see the requests for documents.");
        }

        var query = _context.DocumentRequests.AsNoTracking();

        if (request.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        if (request.EmployeeId is { } employeeId)
        {
            query = query.Where(r => r.EmployeeId == employeeId);
        }

        if (request.Overdue)
        {
            var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

            query = query.Where(r => r.DueDate != null && r.DueDate < today
                && (r.Status == DocumentRequestStatus.Open || r.Status == DocumentRequestStatus.Returned));
        }

        // What needs somebody's eyes first: sent and waiting, then open, then the rest, oldest first.
        var ordered = query
            .OrderBy(r => r.Status == DocumentRequestStatus.Submitted ? 0
                : r.Status == DocumentRequestStatus.Returned ? 1
                : r.Status == DocumentRequestStatus.Open ? 2 : 3)
            .ThenBy(r => r.DueDate == null)
            .ThenBy(r => r.DueDate)
            .ThenByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id);

        return await PagedList<DocumentRequestDto>.CreateAsync(
            ordered.Select(DocumentRequestMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
