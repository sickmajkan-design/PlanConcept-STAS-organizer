using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.VehicleTolls.Models;

public class VehicleTollDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string Type { get; init; } = null!;

    public string Country { get; init; } = null!;

    public string? RouteSegment { get; init; }

    public string Status { get; init; } = null!;

    public DateOnly? ValidUntil { get; init; }

    /// <summary>
    /// "Paid" | "ExpiringSoon" | "Expired" | "Unpaid" — computed from
    /// <see cref="Status"/> and <see cref="ValidUntil"/> against today, the
    /// same way <see cref="VehicleToll.IsExpiredOn"/> and
    /// <see cref="VehicleToll.IsExpiringSoonOn"/> do, so it can never
    /// disagree with them and never goes stale in storage.
    /// </summary>
    public string ComputedState { get; init; } = null!;

    public string? PaidByUserName { get; init; }

    public DateTime? PaidAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="VehicleToll"/> becomes a <see cref="VehicleTollDto"/>.
/// </summary>
/// <remarks>
/// Deliberately not an EF-translatable <c>Expression&lt;Func&gt;</c> the way
/// <c>VehicleMapping.Projection</c> is: <see cref="ComputeState"/> needs
/// "today" and calls entity methods no SQL provider can translate. A
/// vehicle's toll list is a handful of rows at most, so
/// <c>GetVehicleTollsQuery</c> loads the entities and maps them in memory
/// with <see cref="ToDto"/> instead of pushing the computation into a query.
/// </remarks>
public static class VehicleTollMapping
{
    public static VehicleTollDto ToDto(VehicleToll toll, DateOnly today) =>
        new()
        {
            Id = toll.Id,
            VehicleId = toll.VehicleId,
            Type = toll.Type.ToString(),
            Country = toll.Country,
            RouteSegment = toll.RouteSegment,
            Status = toll.Status.ToString(),
            ValidUntil = toll.ValidUntil,
            ComputedState = ComputeState(toll, today),
            PaidByUserName = toll.PaidByUser?.Email,
            PaidAt = toll.PaidAt,
            CreatedAt = toll.CreatedAt,
            UpdatedAt = toll.UpdatedAt,
        };

    public static string ComputeState(VehicleToll toll, DateOnly today) =>
        toll.Status == VehicleTollStatus.Unpaid
            ? "Unpaid"
            : toll.IsExpiredOn(today)
                ? "Expired"
                : toll.IsExpiringSoonOn(today)
                    ? "ExpiringSoon"
                    : "Paid";
}
