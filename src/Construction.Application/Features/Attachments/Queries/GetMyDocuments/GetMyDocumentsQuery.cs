using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments.Models;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Queries.GetMyDocuments;

/// <summary>One toll a vehicle carries, as its driver needs to see it: where, until when, paid or not.</summary>
public class MyVehicleTollDto
{
    public VehicleTollType Type { get; init; }

    public string Country { get; init; } = null!;

    public string? RouteSegment { get; init; }

    public VehicleTollStatus Status { get; init; }

    public DateOnly? ValidUntil { get; init; }
}

/// <summary>
/// A vehicle assigned to the caller. Only what a driver needs for the road — dates and shared
/// documents — never costs, rates or anything about money.
/// </summary>
public class MyVehicleDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string RegistrationNumber { get; init; } = null!;

    public DateOnly? RegistrationValidUntil { get; init; }

    public DateOnly? TechnicalInspectionValidUntil { get; init; }

    public DateOnly? InsuranceValidUntil { get; init; }

    public IReadOnlyList<MyVehicleTollDto> Tolls { get; init; } = [];

    public IReadOnlyList<AttachmentDto> Documents { get; init; } = [];
}

public class MyDocumentsDto
{
    /// <summary>Documents filed on the caller's own record that the office shared with them.</summary>
    public IReadOnlyList<AttachmentDto> Documents { get; init; } = [];

    public IReadOnlyList<MyVehicleDto> Vehicles { get; init; } = [];
}

/// <summary>Everything that is the caller's: their shared documents and the vehicles they drive.</summary>
/// <remarks>
/// Identity comes from the token alone — there is no employee id to pass, so there is nothing to
/// guess. An account with no employee record gets an empty answer, not an error.
/// </remarks>
public record GetMyDocumentsQuery : IRequest<MyDocumentsDto>;

public class GetMyDocumentsQueryHandler : IRequestHandler<GetMyDocumentsQuery, MyDocumentsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyDocumentsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MyDocumentsDto> Handle(
        GetMyDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.EmployeeId is not { } employeeId)
        {
            return new MyDocumentsDto();
        }

        var documents = await _context.Attachments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.SharedWithEmployee)
            .OrderBy(a => a.ExpiresAt == null)
            .ThenBy(a => a.ExpiresAt)
            .ThenByDescending(a => a.CreatedAt)
            .Select(AttachmentMapping.Projection)
            .ToListAsync(cancellationToken);

        var vehicles = await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.AssignedEmployeeId == employeeId)
            .OrderBy(v => v.Brand)
            .ThenBy(v => v.Model)
            .Select(v => new
            {
                v.Id,
                v.Brand,
                v.Model,
                v.RegistrationNumber,
                v.RegistrationValidUntil,
                v.TechnicalInspectionValidUntil,
                v.InsuranceValidUntil
            })
            .ToListAsync(cancellationToken);

        var vehicleIds = vehicles.Select(v => v.Id).ToList();

        var tolls = await _context.VehicleTolls
            .AsNoTracking()
            .Where(t => vehicleIds.Contains(t.VehicleId))
            .OrderBy(t => t.Country)
            .Select(t => new
            {
                t.VehicleId,
                t.Type,
                t.Country,
                t.RouteSegment,
                t.Status,
                t.ValidUntil
            })
            .ToListAsync(cancellationToken);

        var vehicleDocuments = await _context.Attachments
            .AsNoTracking()
            .Where(a => a.VehicleId != null && vehicleIds.Contains(a.VehicleId.Value) && a.SharedWithEmployee)
            .OrderBy(a => a.ExpiresAt == null)
            .ThenBy(a => a.ExpiresAt)
            .ThenByDescending(a => a.CreatedAt)
            .Select(AttachmentMapping.Projection)
            .ToListAsync(cancellationToken);

        return new MyDocumentsDto
        {
            Documents = documents,
            Vehicles = vehicles
                .Select(v => new MyVehicleDto
                {
                    Id = v.Id,
                    Name = $"{v.Brand} {v.Model}",
                    RegistrationNumber = v.RegistrationNumber,
                    RegistrationValidUntil = v.RegistrationValidUntil,
                    TechnicalInspectionValidUntil = v.TechnicalInspectionValidUntil,
                    InsuranceValidUntil = v.InsuranceValidUntil,
                    Tolls = tolls
                        .Where(t => t.VehicleId == v.Id)
                        .Select(t => new MyVehicleTollDto
                        {
                            Type = t.Type,
                            Country = t.Country,
                            RouteSegment = t.RouteSegment,
                            Status = t.Status,
                            ValidUntil = t.ValidUntil
                        })
                        .ToList(),
                    Documents = vehicleDocuments.Where(d => d.OwnerId == v.Id).ToList()
                })
                .ToList()
        };
    }
}
