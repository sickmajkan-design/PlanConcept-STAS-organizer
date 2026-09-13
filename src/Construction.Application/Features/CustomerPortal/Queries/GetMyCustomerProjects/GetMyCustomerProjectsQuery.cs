using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.CustomerPortal.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.CustomerPortal.Queries.GetMyCustomerProjects;

/// <summary>
/// The signed-in customer's own projects, status only. There is no
/// <c>CustomerId</c> parameter: the portal shows one customer's own
/// projects, never lets one ask for another's by id.
/// </summary>
public record GetMyCustomerProjectsQuery : IRequest<IReadOnlyList<CustomerProjectStatusDto>>;

public class GetMyCustomerProjectsQueryHandler
    : IRequestHandler<GetMyCustomerProjectsQuery, IReadOnlyList<CustomerProjectStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyCustomerProjectsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<CustomerProjectStatusDto>> Handle(
        GetMyCustomerProjectsQuery request, CancellationToken cancellationToken)
    {
        var customerId = _currentUserService.CustomerId
            ?? throw new ForbiddenAccessException("This account is not linked to a customer.");

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Status,
                p.Address,
                p.StartDate,
                p.EndDate,
                Total = p.WorkItems.Count(w => w.Status != WorkItemStatus.Cancelled),
                Done = p.WorkItems.Count(w =>
                    w.Status == WorkItemStatus.Resolved || w.Status == WorkItemStatus.Closed)
            })
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return projects
            .Select(p => new CustomerProjectStatusDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                Status = p.Status,
                Address = p.Address,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                PercentComplete = p.Total == 0
                    ? null
                    : (int)Math.Round(p.Done * 100.0 / p.Total)
            })
            .ToList();
    }
}
