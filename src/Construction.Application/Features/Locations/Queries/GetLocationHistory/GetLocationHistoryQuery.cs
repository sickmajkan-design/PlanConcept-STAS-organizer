using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Locations.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Locations.Queries.GetLocationHistory;

/// <summary>
/// Paged movement history for one employee, newest first, optionally limited
/// to a time window (used for route playback in the admin).
/// </summary>
public record GetLocationHistoryQuery : IPagedQuery, IRequest<PagedList<LocationRecordDto>>
{
    /// <summary>Set by the API layer from the route.</summary>
    public Guid EmployeeId { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 100;
}

public class GetLocationHistoryQueryValidator : PagedQueryValidator<GetLocationHistoryQuery>
{
    public GetLocationHistoryQueryValidator()
        : base(maxPageSize: 1000)
    {
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("'From' must not be after 'To'.")
            .OverridePropertyName(nameof(GetLocationHistoryQuery.From));
    }
}

public class GetLocationHistoryQueryHandler
    : IRequestHandler<GetLocationHistoryQuery, PagedList<LocationRecordDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLocationHistoryQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedList<LocationRecordDto>> Handle(
        GetLocationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            throw new NotFoundException(nameof(Employee), request.EmployeeId);
        }

        var query = _context.LocationRecords
            .AsNoTracking()
            .Where(l => l.EmployeeId == request.EmployeeId);

        if (request.From is { } from)
        {
            query = query.Where(l => l.Timestamp >= AsUtc(from));
        }

        if (request.To is { } to)
        {
            query = query.Where(l => l.Timestamp <= AsUtc(to));
        }

        return await PagedList<LocationRecordDto>.CreateAsync(
            query
                .OrderByDescending(l => l.Timestamp)
                .ProjectTo<LocationRecordDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
