using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Vehicles.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Vehicles.Queries.GetVehicles;

public record GetVehiclesQuery : ISortablePagedQuery, IRequest<PagedList<VehicleDto>>
{
    public static readonly string[] AllowedSortFields =
    [
        "brand", "model", "registrationNumber", "fuelType", "status", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches brand, model, registration number and VIN (case-insensitive).</summary>
    public string? Search { get; init; }

    public VehicleStatus? Status { get; init; }

    public FuelType? FuelType { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    /// <summary>When true, returns only vehicles with no assigned employee.</summary>
    public bool? Unassigned { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; }
}

public class GetVehiclesQueryValidator : SortablePagedQueryValidator<GetVehiclesQuery>
{
    public GetVehiclesQueryValidator()
        : base(GetVehiclesQuery.AllowedSortFields)
    {
    }
}

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, PagedList<VehicleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVehiclesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedList<VehicleDto>> Handle(
        GetVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Vehicles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);

            query = query.Where(v =>
                EF.Functions.Like((v.Brand + " " + v.Model).ToLower(), pattern) ||
                EF.Functions.Like(v.RegistrationNumber.ToLower(), pattern) ||
                (v.Vin != null && EF.Functions.Like(v.Vin.ToLower(), pattern)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(v => v.Status == status);
        }

        if (request.FuelType is { } fuelType)
        {
            query = query.Where(v => v.FuelType == fuelType);
        }

        if (request.AssignedEmployeeId is { } employeeId)
        {
            query = query.Where(v => v.AssignedEmployeeId == employeeId);
        }

        if (request.Unassigned == true)
        {
            query = query.Where(v => v.AssignedEmployeeId == null);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<VehicleDto>.CreateAsync(
            query.ProjectTo<VehicleDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static IQueryable<Vehicle> ApplySorting(
        IQueryable<Vehicle> query,
        string? sortBy,
        bool descending)
    {
        IOrderedQueryable<Vehicle> ordered = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("model", false) => query.OrderBy(v => v.Model),
            ("model", true) => query.OrderByDescending(v => v.Model),
            ("registrationnumber", false) => query.OrderBy(v => v.RegistrationNumber),
            ("registrationnumber", true) => query.OrderByDescending(v => v.RegistrationNumber),
            ("fueltype", false) => query.OrderBy(v => v.FuelType),
            ("fueltype", true) => query.OrderByDescending(v => v.FuelType),
            ("status", false) => query.OrderBy(v => v.Status),
            ("status", true) => query.OrderByDescending(v => v.Status),
            ("createdat", false) => query.OrderBy(v => v.CreatedAt),
            ("createdat", true) => query.OrderByDescending(v => v.CreatedAt),
            (_, true) => query.OrderByDescending(v => v.Brand).ThenByDescending(v => v.Model),
            _ => query.OrderBy(v => v.Brand).ThenBy(v => v.Model)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(v => v.Id);
    }
}
