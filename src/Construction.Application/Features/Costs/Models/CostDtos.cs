using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Costs.Models;

public class EmployeeRateDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public decimal HourlyRate { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }

    public string? SetByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class MaterialMovementDto
{
    public Guid Id { get; init; }

    public Guid MaterialId { get; init; }

    public string MaterialName { get; init; } = null!;

    public string Unit { get; init; } = null!;

    public MaterialMovementKind Kind { get; init; }

    public decimal Quantity { get; init; }

    public decimal? UnitPrice { get; init; }

    public decimal? TotalCost { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class VehicleExpenseDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string VehicleName { get; init; } = null!;

    public VehicleExpenseKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public decimal? Litres { get; init; }

    public decimal? PricePerLitre { get; init; }

    public int? OdometerKm { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>How an <see cref="EmployeeRate"/> becomes an <see cref="EmployeeRateDto"/>.</summary>
/// <remarks>See <c>EmployeeMapping</c> for the convention these all follow.</remarks>
public static class EmployeeRateMapping
{
    public static readonly Expression<Func<EmployeeRate, EmployeeRateDto>> Projection = rate =>
        new EmployeeRateDto
        {
            Id = rate.Id,
            EmployeeId = rate.EmployeeId,
            EmployeeName = rate.Employee.FirstName + " " + rate.Employee.LastName,
            HourlyRate = rate.HourlyRate,
            StartDate = rate.StartDate,
            EndDate = rate.EndDate,
            Note = rate.Note,
            SetByName = rate.SetByUser != null ? rate.SetByUser.Email : null,
            CreatedAt = rate.CreatedAt,
        };

    private static readonly Func<EmployeeRate, EmployeeRateDto> Compiled = Projection.Compile();

    public static EmployeeRateDto ToDto(EmployeeRate rate) => Compiled(rate);
}

/// <summary>How a <see cref="MaterialMovement"/> becomes a <see cref="MaterialMovementDto"/>.</summary>
public static class MaterialMovementMapping
{
    public static readonly Expression<Func<MaterialMovement, MaterialMovementDto>> Projection =
        movement => new MaterialMovementDto
        {
            Id = movement.Id,
            MaterialId = movement.MaterialId,
            MaterialName = movement.Material.Name,
            Unit = movement.Material.Unit,
            Kind = movement.Kind,
            Quantity = movement.Quantity,
            UnitPrice = movement.UnitPrice,
            // Spelled out rather than taken from the entity's computed
            // property, which cannot be turned into SQL.
            TotalCost = movement.UnitPrice != null
                ? movement.UnitPrice * (movement.Quantity < 0 ? -movement.Quantity : movement.Quantity)
                : (decimal?)null,
            ProjectId = movement.ProjectId,
            ProjectName = movement.Project != null ? movement.Project.Name : null,
            OccurredOn = movement.OccurredOn,
            Note = movement.Note,
            RecordedByName = movement.RecordedByUser != null ? movement.RecordedByUser.Email : null,
            CreatedAt = movement.CreatedAt,
        };

    private static readonly Func<MaterialMovement, MaterialMovementDto> Compiled =
        Projection.Compile();

    public static MaterialMovementDto ToDto(MaterialMovement movement) => Compiled(movement);
}

/// <summary>How a <see cref="VehicleExpense"/> becomes a <see cref="VehicleExpenseDto"/>.</summary>
public static class VehicleExpenseMapping
{
    public static readonly Expression<Func<VehicleExpense, VehicleExpenseDto>> Projection =
        expense => new VehicleExpenseDto
        {
            Id = expense.Id,
            VehicleId = expense.VehicleId,
            VehicleName = expense.Vehicle.Brand + " " + expense.Vehicle.Model
                + " (" + expense.Vehicle.RegistrationNumber + ")",
            Kind = expense.Kind,
            Amount = expense.Amount,
            OccurredOn = expense.OccurredOn,
            Litres = expense.Litres,
            PricePerLitre = expense.Litres != null && expense.Litres > 0
                ? expense.Amount / expense.Litres
                : (decimal?)null,
            OdometerKm = expense.OdometerKm,
            Supplier = expense.Supplier,
            Note = expense.Note,
            RecordedByName = expense.RecordedByUser != null ? expense.RecordedByUser.Email : null,
            CreatedAt = expense.CreatedAt,
        };

    private static readonly Func<VehicleExpense, VehicleExpenseDto> Compiled = Projection.Compile();

    public static VehicleExpenseDto ToDto(VehicleExpense expense) => Compiled(expense);
}
