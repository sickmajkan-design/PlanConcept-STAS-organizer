using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Finance;

/// <summary>Money received that belongs to no project.</summary>
public class CompanyRevenueDto
{
    public Guid Id { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    /// <summary>"VehicleRental", "ToolRental" or "Other".</summary>
    public string Source { get; init; } = null!;

    public Guid? VehicleId { get; init; }

    /// <summary>"Brand Model (registration)", so the list needs no second call.</summary>
    public string? VehicleName { get; init; }

    public Guid? ToolId { get; init; }

    public string? ToolName { get; init; }

    public string? Note { get; init; }

    public DateTime CreatedAt { get; init; }
}

public static class CompanyRevenueMapping
{
    public static readonly Expression<Func<CompanyRevenue, CompanyRevenueDto>> Projection = r => new CompanyRevenueDto
    {
        Id = r.Id,
        Amount = r.Amount,
        OccurredOn = r.OccurredOn,
        Source = r.Source.ToString(),
        VehicleId = r.VehicleId,
        VehicleName = r.Vehicle == null
            ? null
            : r.Vehicle.Brand + " " + r.Vehicle.Model + " (" + r.Vehicle.RegistrationNumber + ")",
        ToolId = r.ToolId,
        ToolName = r.Tool == null ? null : r.Tool.Name,
        Note = r.Note,
        CreatedAt = r.CreatedAt,
    };
}
