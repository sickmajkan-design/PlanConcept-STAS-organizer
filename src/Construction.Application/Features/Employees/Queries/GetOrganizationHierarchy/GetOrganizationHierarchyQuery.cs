using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Queries.GetOrganizationHierarchy;

/// <summary>One person, as the org chart shows them.</summary>
public class OrganizationHierarchyNodeDto
{
    public Guid EmployeeId { get; init; }

    public string FullName { get; init; } = null!;

    public string Position { get; init; } = null!;

    /// <summary>The rank picked by hand, or null — the page then places them by <see cref="Role"/>.</summary>
    public OrganizationRank? Rank { get; init; }

    /// <summary>
    /// Null for an employee with no login of their own — a subcontractor, most
    /// often — who therefore has no tier to sit at above "worker" in this view.
    /// </summary>
    public string? Role { get; init; }
}

/// <summary>A login that belongs to no employee record, so it has nowhere to hold a rank.</summary>
public class UnlinkedAccountDto
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = null!;

    public string Role { get; init; } = null!;
}

public class OrganizationHierarchyDto
{
    public List<OrganizationHierarchyNodeDto> People { get; init; } = new();

    /// <summary>
    /// Active staff logins with no employee behind them. Listed so they are not
    /// invisible, not placed: a rank is a field on the employee.
    /// </summary>
    public List<UnlinkedAccountDto> UnlinkedAccounts { get; init; } = new();
}

/// <summary>
/// Everyone active, for the frontend to bucket into tiers by rank.
/// </summary>
/// <remarks>
/// The grouping itself is left to the page rather than done here: it is
/// display logic (tier labels, ordering, which roles collapse into "worker"),
/// not a query concern, and it is the one place in the app that already knows
/// how to translate a role name.
/// </remarks>
public record GetOrganizationHierarchyQuery : IRequest<OrganizationHierarchyDto>;

public class GetOrganizationHierarchyQueryHandler
    : IRequestHandler<GetOrganizationHierarchyQuery, OrganizationHierarchyDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrganizationHierarchyQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<OrganizationHierarchyDto> Handle(
        GetOrganizationHierarchyQuery request,
        CancellationToken cancellationToken)
    {
        var people = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new OrganizationHierarchyNodeDto
            {
                EmployeeId = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                Position = e.Position,
                Rank = e.Rank,
                Role = e.User != null ? e.User.Role.ToString() : null,
            })
            .ToListAsync(cancellationToken);

        // Login emails and roles are what the Users screen shows, and that
        // screen is Admin and above. This endpoint is open one tier lower so a
        // foreman can see the chart, so the accounts are withheld from them
        // rather than widening who may read them.
        var unlinked = new List<UnlinkedAccountDto>();

        if (_currentUserService.Role is UserRole.SuperAdmin or UserRole.Admin)
        {
            unlinked = await _context.Users
                .AsNoTracking()
                .Where(u => u.EmployeeId == null
                    && u.IsActive
                    // A customer's portal login is not staff.
                    && u.Role != UserRole.Customer)
                .OrderBy(u => u.Role)
                .ThenBy(u => u.Email)
                .Select(u => new UnlinkedAccountDto
                {
                    UserId = u.Id,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                })
                .ToListAsync(cancellationToken);
        }

        return new OrganizationHierarchyDto { People = people, UnlinkedAccounts = unlinked };
    }
}
