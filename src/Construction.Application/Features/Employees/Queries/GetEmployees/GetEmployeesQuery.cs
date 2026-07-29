using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Employees.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Employees.Queries.GetEmployees;

public record GetEmployeesQuery : IRequest<PagedList<EmployeeDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "employeeNumber", "firstName", "lastName", "position", "status", "employmentDate", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches name, employee number, email and position (case-insensitive).</summary>
    public string? Search { get; init; }

    public EmployeeStatus? Status { get; init; }

    public string? Position { get; init; }

    /// <summary>Restricts results to employees assigned to the given project.</summary>
    public Guid? ProjectId { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) ||
                            GetEmployeesQuery.AllowedSortFields.Contains(
                                sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                $"SortBy must be one of: {string.Join(", ", GetEmployeesQuery.AllowedSortFields)}.");
    }
}

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, PagedList<EmployeeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEmployeesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedList<EmployeeDto>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim().ToLowerInvariant()}%";

            query = query.Where(e =>
                EF.Functions.Like((e.FirstName + " " + e.LastName).ToLower(), pattern) ||
                EF.Functions.Like(e.EmployeeNumber.ToLower(), pattern) ||
                EF.Functions.Like(e.Position.ToLower(), pattern) ||
                (e.Email != null && EF.Functions.Like(e.Email.ToLower(), pattern)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Position))
        {
            query = query.Where(e => EF.Functions.Like(
                e.Position.ToLower(), $"%{request.Position.Trim().ToLowerInvariant()}%"));
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(e => e.ProjectAssignments.Any(pa => pa.ProjectId == projectId));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<EmployeeDto>.CreateAsync(
            query.ProjectTo<EmployeeDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Employee> ApplySorting(
        IQueryable<Employee> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Employee> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("employeenumber", false) => query.OrderBy(e => e.EmployeeNumber),
            ("employeenumber", true) => query.OrderByDescending(e => e.EmployeeNumber),
            ("firstname", false) => query.OrderBy(e => e.FirstName),
            ("firstname", true) => query.OrderByDescending(e => e.FirstName),
            ("position", false) => query.OrderBy(e => e.Position),
            ("position", true) => query.OrderByDescending(e => e.Position),
            ("status", false) => query.OrderBy(e => e.Status),
            ("status", true) => query.OrderByDescending(e => e.Status),
            ("employmentdate", false) => query.OrderBy(e => e.EmploymentDate),
            ("employmentdate", true) => query.OrderByDescending(e => e.EmploymentDate),
            ("createdat", false) => query.OrderBy(e => e.CreatedAt),
            ("createdat", true) => query.OrderByDescending(e => e.CreatedAt),
            (_, true) => query.OrderByDescending(e => e.LastName).ThenByDescending(e => e.FirstName),
            _ => query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(e => e.Id);
    }
}
