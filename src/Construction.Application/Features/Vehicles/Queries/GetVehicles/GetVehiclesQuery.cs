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
        "brand", "model", "registrationNumber", "fuelType", "status", "ownershipType", "assignedEmployeeName", "currentRentalOutRenterName", "lastRentalOutRenterName", "createdAt"
    ];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    /// <summary>Matches brand, model, registration number and VIN (case-insensitive).</summary>
    public string? Search { get; init; }

    public VehicleStatus? Status { get; init; }

    public FuelType? FuelType { get; init; }

    public VehicleOwnershipType? OwnershipType { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    public Guid? AssignedProjectId { get; init; }

    /// <summary>When true, returns only vehicles with no assigned employee.</summary>
    public bool? Unassigned { get; init; }

    /// <summary>When true, returns only vehicles missing a VIN or QR code.</summary>
    public bool? IncompleteOnly { get; init; }

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

    public GetVehiclesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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
                EF.Functions.Like((v.Brand + " " + v.Model).ToLower(), pattern, SearchPattern.Escape) ||
                EF.Functions.Like(v.RegistrationNumber.ToLower(), pattern, SearchPattern.Escape) ||
                (v.Vin != null && EF.Functions.Like(v.Vin.ToLower(), pattern, SearchPattern.Escape)));
        }

        if (request.Status is { } status)
        {
            query = query.Where(v => v.Status == status);
        }

        if (request.FuelType is { } fuelType)
        {
            query = query.Where(v => v.FuelType == fuelType);
        }

        if (request.OwnershipType is { } ownershipType)
        {
            query = query.Where(v => v.OwnershipType == ownershipType);
        }

        if (request.AssignedEmployeeId is { } employeeId)
        {
            query = query.Where(v => v.AssignedEmployeeId == employeeId);
        }

        if (request.AssignedProjectId is { } projectId)
        {
            query = query.Where(v => v.AssignedProjectId == projectId);
        }

        if (request.Unassigned == true)
        {
            query = query.Where(v => v.AssignedEmployeeId == null);
        }

        if (request.IncompleteOnly == true)
        {
            query = query.Where(v => v.Vin == null || v.QrCode == null);
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        return await PagedList<VehicleDto>.CreateAsync(
            query.Select(VehicleMapping.Projection),
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
            ("ownershiptype", false) => query.OrderBy(v => v.OwnershipType),
            ("ownershiptype", true) => query.OrderByDescending(v => v.OwnershipType),
            ("assignedemployeename", false) => query
                .OrderBy(v => v.AssignedEmployee != null ? v.AssignedEmployee.LastName : null)
                .ThenBy(v => v.AssignedEmployee != null ? v.AssignedEmployee.FirstName : null),
            ("assignedemployeename", true) => query
                .OrderByDescending(v => v.AssignedEmployee != null ? v.AssignedEmployee.LastName : null)
                .ThenByDescending(v => v.AssignedEmployee != null ? v.AssignedEmployee.FirstName : null),
            ("currentrentaloutrentername", false) => query
                .OrderBy(v => v.RentalsOut
                    .Where(r => r.EndDate == null)
                    .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                    .FirstOrDefault()),
            ("currentrentaloutrentername", true) => query
                .OrderByDescending(v => v.RentalsOut
                    .Where(r => r.EndDate == null)
                    .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                    .FirstOrDefault()),
            ("lastrentaloutrentername", false) => query
                .OrderBy(v => v.RentalsOut
                    .Where(r => r.EndDate != null)
                    .OrderByDescending(r => r.EndDate)
                    .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                    .FirstOrDefault()),
            ("lastrentaloutrentername", true) => query
                .OrderByDescending(v => v.RentalsOut
                    .Where(r => r.EndDate != null)
                    .OrderByDescending(r => r.EndDate)
                    .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                    .FirstOrDefault()),
            ("createdat", false) => query.OrderBy(v => v.CreatedAt),
            ("createdat", true) => query.OrderByDescending(v => v.CreatedAt),
            (_, true) => query.OrderByDescending(v => v.Brand).ThenByDescending(v => v.Model),
            _ => query.OrderBy(v => v.Brand).ThenBy(v => v.Model)
        };

        // Stable tiebreaker so pagination never skips or duplicates rows.
        return ordered.ThenBy(v => v.Id);
    }
}
