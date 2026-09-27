using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Absences.Models;

/// <summary>One manual correction of somebody's annual leave.</summary>
public class LeaveAdjustmentDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public int Year { get; init; }

    public int Days { get; init; }

    public string Reason { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    /// <summary>Who wrote it, by e-mail, when the account still exists.</summary>
    public string? CreatedBy { get; init; }
}

public static class LeaveAdjustmentMapping
{
    public static Expression<Func<LeaveAdjustment, LeaveAdjustmentDto>> Projection =>
        a => new LeaveAdjustmentDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            Year = a.Year,
            Days = a.Days,
            Reason = a.Reason,
            CreatedAt = a.CreatedAt,
            CreatedBy = a.CreatedByUser != null ? a.CreatedByUser.Email : null,
        };
}
