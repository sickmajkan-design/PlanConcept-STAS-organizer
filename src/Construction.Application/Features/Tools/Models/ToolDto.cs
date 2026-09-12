using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Tools.Models;

public class ToolDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Category { get; init; }

    public string? SerialNumber { get; init; }

    public string? QrCode { get; init; }

    public string Status { get; init; } = null!;

    public string OwnershipType { get; init; } = null!;

    /// <summary>Set when a rental/lease rate is currently in force (EndDate null). Null for an owned tool, or one with no rate on file.</summary>
    public decimal? CurrentRentalMonthlyAmount { get; init; }

    public string? CurrentRentalProvider { get; init; }

    /// <summary>Set when this tool is currently loaned out to another company (EndDate null on the rental-out row). Null otherwise.</summary>
    public string? CurrentRentalOutRenterName { get; init; }

    public decimal? CurrentRentalOutDailyRate { get; init; }

    public DateOnly? CurrentRentalOutStartDate { get; init; }

    /// <summary>Renter on the most recently closed rental-out loan (EndDate not null). Null if never loaned out.</summary>
    public string? LastRentalOutRenterName { get; init; }

    public DateOnly? LastRentalOutEndDate { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public string? AssignedEmployeeNumber { get; init; }

    public Guid? AssignedProjectId { get; init; }

    public string? AssignedProjectName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Tool"/> becomes an <see cref="ToolDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class ToolMapping
{
    public static readonly Expression<Func<Tool, ToolDto>> Projection = tool =>
        new ToolDto
        {
            Id = tool.Id,
            Name = tool.Name,
            Category = tool.Category,
            SerialNumber = tool.SerialNumber,
            QrCode = tool.QrCode,
            Status = tool.Status.ToString(),
            OwnershipType = tool.OwnershipType.ToString(),
            CurrentRentalMonthlyAmount = tool.RentalRates
                .Where(r => r.EndDate == null)
                .Select(r => (decimal?)r.MonthlyAmount)
                .FirstOrDefault(),
            CurrentRentalProvider = tool.RentalRates
                .Where(r => r.EndDate == null)
                .Select(r => r.Provider)
                .FirstOrDefault(),
            CurrentRentalOutRenterName = tool.RentalsOut
                .Where(r => r.EndDate == null)
                .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                .FirstOrDefault(),
            CurrentRentalOutDailyRate = tool.RentalsOut
                .Where(r => r.EndDate == null)
                .Select(r => (decimal?)r.DailyRate)
                .FirstOrDefault(),
            CurrentRentalOutStartDate = tool.RentalsOut
                .Where(r => r.EndDate == null)
                .Select(r => (DateOnly?)r.StartDate)
                .FirstOrDefault(),
            LastRentalOutRenterName = tool.RentalsOut
                .Where(r => r.EndDate != null)
                .OrderByDescending(r => r.EndDate)
                .Select(r => r.Customer != null ? r.Customer.Name : r.RenterName)
                .FirstOrDefault(),
            LastRentalOutEndDate = tool.RentalsOut
                .Where(r => r.EndDate != null)
                .OrderByDescending(r => r.EndDate)
                .Select(r => (DateOnly?)r.EndDate)
                .FirstOrDefault(),
            AssignedEmployeeId = tool.AssignedEmployeeId,
            AssignedEmployeeName = tool.AssignedEmployee != null
                ? tool.AssignedEmployee.FirstName + " " + tool.AssignedEmployee.LastName
                : null,
            AssignedEmployeeNumber = tool.AssignedEmployee != null
                ? tool.AssignedEmployee.EmployeeNumber
                : null,
            AssignedProjectId = tool.AssignedProjectId,
            AssignedProjectName = tool.AssignedProject != null ? tool.AssignedProject.Name : null,
            CreatedAt = tool.CreatedAt,
            UpdatedAt = tool.UpdatedAt,
        };

    private static readonly Func<Tool, ToolDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static ToolDto ToDto(Tool tool) => Compiled(tool);
}
