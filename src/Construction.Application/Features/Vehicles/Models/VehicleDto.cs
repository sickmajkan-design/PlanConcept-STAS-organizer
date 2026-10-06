using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Vehicles.Models;

public class VehicleDto
{
    public Guid Id { get; init; }

    public string Brand { get; init; } = null!;

    public string Model { get; init; } = null!;

    public string RegistrationNumber { get; init; } = null!;

    /// <summary>Null only for a vehicle created before the field existed; it must be filled in on its next edit.</summary>
    public string? TdNumber { get; init; }

    public string? Vin { get; init; }

    public string? QrCode { get; init; }

    public string? GpsProvider { get; init; }

    public string? GpsTrackingUrl { get; init; }

    /// <summary>The last day the registration is valid. Null when not entered.</summary>
    public DateOnly? RegistrationValidUntil { get; init; }

    /// <summary>The last day of the rental or lease. Null for an owned vehicle or when no end is agreed.</summary>
    public DateOnly? RentedUntil { get; init; }

    public string FuelType { get; init; } = null!;

    public string Status { get; init; } = null!;

    public string OwnershipType { get; init; } = null!;

    /// <summary>Set when a rental/lease rate is currently in force (EndDate null). Null for an owned vehicle, or one with no rate on file.</summary>
    public decimal? CurrentRentalMonthlyAmount { get; init; }

    public string? CurrentRentalProvider { get; init; }

    /// <summary>Set when this vehicle is currently loaned out to another company (EndDate null on the rental-out row). Null otherwise.</summary>
    public string? CurrentRentalOutRenterName { get; init; }

    public decimal? CurrentRentalOutDailyRate { get; init; }

    public DateOnly? CurrentRentalOutStartDate { get; init; }

    /// <summary>When the vehicle that is out is due back, if agreed.</summary>
    public DateOnly? CurrentRentalOutExpectedEndDate { get; init; }

    /// <summary>Renter on the most recently closed rental-out loan (EndDate not null). Null if never loaned out.</summary>
    public string? LastRentalOutRenterName { get; init; }

    public DateOnly? LastRentalOutEndDate { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public string? AssignedEmployeeNumber { get; init; }

    /// <summary>The unit the record belongs to in its own right. Null: it follows its project.</summary>
    public Guid? BranchId { get; init; }

    public string? BranchName { get; init; }

    public string? BranchColor { get; init; }

    public Guid? AssignedProjectId { get; init; }

    public string? AssignedProjectName { get; init; }

    /// <summary>
    /// True when this vehicle has at least one toll (vignette, tunnel,
    /// road-passage charge) that is unpaid, expired, or expiring soon. Lets
    /// the vehicles list show a warning badge without a second request per
    /// row for the full toll list.
    /// </summary>
    public bool HasExpiredOrExpiringTolls { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Vehicle"/> becomes an <see cref="VehicleDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
///
/// Takes <c>today</c> as a parameter, captured into the expression as a
/// closure, rather than reading <see cref="DateTime.UtcNow"/> inside it
/// directly — the same pattern <c>GetVehiclesQuery</c> already uses for its
/// own site-scoping, so a caller controls "today" once and every value
/// derived from it agrees, including in tests that fix the clock.
/// </remarks>
public static class VehicleMapping
{
    public static Expression<Func<Vehicle, VehicleDto>> Projection(DateOnly today) => vehicle =>
        new VehicleDto
        {
            Id = vehicle.Id,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            RegistrationNumber = vehicle.RegistrationNumber,
            TdNumber = vehicle.TdNumber,
            Vin = vehicle.Vin,
            QrCode = vehicle.QrCode,
            GpsProvider = vehicle.GpsProvider,
            GpsTrackingUrl = vehicle.GpsTrackingUrl,
            RegistrationValidUntil = vehicle.RegistrationValidUntil,
            RentedUntil = vehicle.RentedUntil,
            FuelType = vehicle.FuelType.ToString(),
            Status = vehicle.Status.ToString(),
            OwnershipType = vehicle.OwnershipType.ToString(),
            CurrentRentalMonthlyAmount = vehicle.RentalRates
                .Where(r => r.EndDate == null)
                .Select(r => (decimal?)r.MonthlyAmount)
                .FirstOrDefault(),
            CurrentRentalProvider = vehicle.RentalRates
                .Where(r => r.EndDate == null)
                .Select(r => r.Provider)
                .FirstOrDefault(),
            CurrentRentalOutRenterName = vehicle.RentalsOut
                .Where(r => r.EndDate == null)
                .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                .FirstOrDefault(),
            CurrentRentalOutDailyRate = vehicle.RentalsOut
                .Where(r => r.EndDate == null)
                .Select(r => (decimal?)r.DailyRate)
                .FirstOrDefault(),
            CurrentRentalOutStartDate = vehicle.RentalsOut
                .Where(r => r.EndDate == null)
                .Select(r => (DateOnly?)r.StartDate)
                .FirstOrDefault(),
            CurrentRentalOutExpectedEndDate = vehicle.RentalsOut
                .Where(r => r.EndDate == null)
                .Select(r => r.ExpectedEndDate)
                .FirstOrDefault(),
            LastRentalOutRenterName = vehicle.RentalsOut
                .Where(r => r.EndDate != null)
                .OrderByDescending(r => r.EndDate)
                .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                .FirstOrDefault(),
            LastRentalOutEndDate = vehicle.RentalsOut
                .Where(r => r.EndDate != null)
                .OrderByDescending(r => r.EndDate)
                .Select(r => (DateOnly?)r.EndDate)
                .FirstOrDefault(),
            AssignedEmployeeId = vehicle.AssignedEmployeeId,
            AssignedEmployeeName = vehicle.AssignedEmployee != null
                ? vehicle.AssignedEmployee.FirstName + " " + vehicle.AssignedEmployee.LastName
                : null,
            AssignedEmployeeNumber = vehicle.AssignedEmployee != null
                ? vehicle.AssignedEmployee.EmployeeNumber
                : null,
            BranchId = vehicle.BranchId,
            BranchName = vehicle.Branch != null ? vehicle.Branch.Name : null,
            BranchColor = vehicle.Branch != null ? vehicle.Branch.Color : null,
            AssignedProjectId = vehicle.AssignedProjectId,
            AssignedProjectName = vehicle.AssignedProject != null ? vehicle.AssignedProject.Name : null,
            // Unpaid, or paid but within (or past) its own expiring-soon
            // window — the same comparisons VehicleToll.IsExpiredOn/
            // IsExpiringSoonOn make, inlined because those instance methods
            // are not themselves translatable to SQL. "<= today + 7" already
            // covers "< today", so a lapsed toll counts too.
            HasExpiredOrExpiringTolls = vehicle.Tolls.Any(t =>
                t.Status == VehicleTollStatus.Unpaid
                || t.ValidUntil == null
                || t.ValidUntil.Value <= today.AddDays(7)),
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt,
        };

    /// <summary>
    /// Maps a record already in memory. <c>today</c> defaults to the real
    /// clock rather than requiring every create/assign/unassign command
    /// handler to take a dependency on <see cref="Construction.Application.Common.Interfaces.IDateTimeProvider"/>
    /// just to answer a display-only flag on the command's response — the
    /// handlers that actually decide anything by "today" (the toll sweep,
    /// the vehicle list/detail queries) already pass it explicitly.
    /// </summary>
    public static VehicleDto ToDto(Vehicle vehicle, DateOnly? today = null) =>
        Projection(today ?? DateOnly.FromDateTime(DateTime.UtcNow)).Compile()(vehicle);
}
