using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Queries.GetOrganizationHierarchy;

/// <summary>One person, as the org chart shows them.</summary>
public class OrganizationHierarchyNodeDto
{
    public Guid EmployeeId { get; init; }

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;

    /// <summary>
    /// Null for an employee with no login of their own — a subcontractor, most
    /// often — who therefore has no tier to sit at above "worker" in this view.
    /// </summary>
    public string? Role { get; init; }
}

/// <summary>
/// Everyone active, for the frontend to bucket into tiers by <see cref="OrganizationHierarchyNodeDto.Role"/>.
/// </summary>
/// <remarks>
/// The grouping itself is left to the page rather than done here: it is
/// display logic (tier labels, ordering, which roles collapse into "worker"),
/// not a query concern, and it is the one place in the app that already knows
/// how to translate a role name.
/// </remarks>
public record GetOrganizationHierarchyQuery : IRequest<List<OrganizationHierarchyNodeDto>>;

public class GetOrganizationHierarchyQueryHandler
    : IRequestHandler<GetOrganizationHierarchyQuery, List<OrganizationHierarchyNodeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetOrganizationHierarchyQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrganizationHierarchyNodeDto>> Handle(
        GetOrganizationHierarchyQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new OrganizationHierarchyNodeDto
            {
                EmployeeId = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                Position = e.Position,
                Role = e.User != null ? e.User.Role.ToString() : null,
            })
            .ToListAsync(cancellationToken);
    }
}
