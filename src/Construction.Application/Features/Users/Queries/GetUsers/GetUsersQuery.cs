using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Users.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery : ISortablePagedQuery, IRequest<PagedList<UserDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "email", "role", "isActive", "lastLoginAt", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches the account email and the linked employee's name.</summary>
    public string? Search { get; init; }

    public UserRole? Role { get; init; }

    /// <summary>
    /// Unset returns both active and deactivated accounts. Offboarding is
    /// reviewed by looking at who still has access, so the default must not
    /// hide anyone.
    /// </summary>
    public bool? IsActive { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetUsersQueryValidator : SortablePagedQueryValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
        : base(GetUsersQuery.AllowedSortFields)
    {
    }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedList<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUsersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedList<UserDto>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(u =>
                EF.Functions.Like(u.Email.ToLower(), pattern, SearchPattern.Escape) ||
                (u.Employee != null && EF.Functions.Like(
                    (u.Employee.FirstName + " " + u.Employee.LastName).ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.Role is { } role)
        {
            query = query.Where(u => u.Role == role);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(u => u.IsActive == isActive);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<UserDto>.CreateAsync(
            query.ProjectTo<UserDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<User> ApplySorting(
        IQueryable<User> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<User> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("email", true) => query.OrderByDescending(u => u.Email),
            ("email", false) => query.OrderBy(u => u.Email),
            ("role", true) => query.OrderByDescending(u => u.Role),
            ("role", false) => query.OrderBy(u => u.Role),
            ("isactive", true) => query.OrderByDescending(u => u.IsActive),
            ("isactive", false) => query.OrderBy(u => u.IsActive),
            ("lastloginat", true) => query.OrderByDescending(u => u.LastLoginAt),
            ("lastloginat", false) => query.OrderBy(u => u.LastLoginAt),
            ("createdat", true) => query.OrderByDescending(u => u.CreatedAt),
            ("createdat", false) => query.OrderBy(u => u.CreatedAt),
            // Deactivated accounts first by default: this list exists to
            // answer "who still has access", and the exceptions are the
            // interesting rows.
            (_, true) => query.OrderByDescending(u => u.IsActive).ThenByDescending(u => u.Email),
            _ => query.OrderBy(u => u.IsActive).ThenBy(u => u.Email)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(u => u.Id);
    }
}
