using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Costs.Models;

public class EmployeeRateDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public RateType RateType { get; init; }

    public decimal? HourlyRate { get; init; }

    public decimal? WeekendHourlyRate { get; init; }

    public decimal? HolidayHourlyRate { get; init; }

    public decimal? DailyRate { get; init; }

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

public class FinanceEntryDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeName { get; init; } = null!;

    public FinanceEntryKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public decimal? HoursWorked { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>The totals for whatever filter is currently applied to the list, not just the page on screen.</summary>
public class EmployeeRateSummaryDto
{
    public int Count { get; init; }

    public decimal? AverageHourlyRate { get; init; }
}

public class VehicleRentalRateSummaryDto
{
    public int Count { get; init; }

    public decimal TotalMonthlyAmount { get; init; }
}

public class MaterialMovementSummaryDto
{
    public int Count { get; init; }

    /// <summary>Sum of every priced movement's value, In and Out mixed together.</summary>
    public decimal TotalCost { get; init; }
}

public class VehicleExpenseSummaryDto
{
    public int Count { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal TotalLitres { get; init; }
}

public class FinanceEntrySummaryDto
{
    public int Count { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal TotalHoursWorked { get; init; }
}

public class ToolExpenseDto
{
    public Guid Id { get; init; }

    public Guid ToolId { get; init; }

    public string ToolName { get; init; } = null!;

    public ToolExpenseKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class ToolExpenseSummaryDto
{
    public int Count { get; init; }

    public decimal TotalAmount { get; init; }
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
            RateType = rate.RateType,
            HourlyRate = rate.HourlyRate,
            WeekendHourlyRate = rate.WeekendHourlyRate,
            HolidayHourlyRate = rate.HolidayHourlyRate,
            DailyRate = rate.DailyRate,
            StartDate = rate.StartDate,
            EndDate = rate.EndDate,
            Note = rate.Note,
            SetByName = rate.SetByUser != null ? rate.SetByUser.Email : null,
            CreatedAt = rate.CreatedAt,
        };

    private static readonly Func<EmployeeRate, EmployeeRateDto> Compiled = Projection.Compile();

    public static EmployeeRateDto ToDto(EmployeeRate rate) => Compiled(rate);
}

public class VehicleRentalRateDto
{
    public Guid Id { get; init; }

    public Guid VehicleId { get; init; }

    public string VehicleName { get; init; } = null!;

    public decimal MonthlyAmount { get; init; }

    public string? Provider { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }

    public string? SetByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>How a <see cref="VehicleRentalRate"/> becomes a <see cref="VehicleRentalRateDto"/>.</summary>
/// <remarks>See <c>EmployeeMapping</c> for the convention these all follow.</remarks>
public static class VehicleRentalRateMapping
{
    public static readonly Expression<Func<VehicleRentalRate, VehicleRentalRateDto>> Projection = rate =>
        new VehicleRentalRateDto
        {
            Id = rate.Id,
            VehicleId = rate.VehicleId,
            VehicleName = rate.Vehicle.Brand + " " + rate.Vehicle.Model
                + " (" + rate.Vehicle.RegistrationNumber + ")",
            MonthlyAmount = rate.MonthlyAmount,
            Provider = rate.Provider,
            StartDate = rate.StartDate,
            EndDate = rate.EndDate,
            Note = rate.Note,
            SetByName = rate.SetByUser != null ? rate.SetByUser.Email : null,
            CreatedAt = rate.CreatedAt,
        };

    private static readonly Func<VehicleRentalRate, VehicleRentalRateDto> Compiled = Projection.Compile();

    public static VehicleRentalRateDto ToDto(VehicleRentalRate rate) => Compiled(rate);
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

/// <summary>How a <see cref="FinanceEntry"/> becomes a <see cref="FinanceEntryDto"/>.</summary>
public static class FinanceEntryMapping
{
    public static readonly Expression<Func<FinanceEntry, FinanceEntryDto>> Projection =
        entry => new FinanceEntryDto
        {
            Id = entry.Id,
            EmployeeId = entry.EmployeeId,
            EmployeeName = entry.Employee.FirstName + " " + entry.Employee.LastName,
            Kind = entry.Kind,
            Amount = entry.Amount,
            OccurredOn = entry.OccurredOn,
            ProjectId = entry.ProjectId,
            ProjectName = entry.Project != null ? entry.Project.Name : null,
            HoursWorked = entry.HoursWorked,
            Note = entry.Note,
            RecordedByName = entry.RecordedByUser != null ? entry.RecordedByUser.Email : null,
            CreatedAt = entry.CreatedAt,
        };

    private static readonly Func<FinanceEntry, FinanceEntryDto> Compiled = Projection.Compile();

    public static FinanceEntryDto ToDto(FinanceEntry entry) => Compiled(entry);
}

/// <summary>How a <see cref="ToolExpense"/> becomes a <see cref="ToolExpenseDto"/>.</summary>
public static class ToolExpenseMapping
{
    public static readonly Expression<Func<ToolExpense, ToolExpenseDto>> Projection =
        expense => new ToolExpenseDto
        {
            Id = expense.Id,
            ToolId = expense.ToolId,
            ToolName = expense.Tool.Name,
            Kind = expense.Kind,
            Amount = expense.Amount,
            OccurredOn = expense.OccurredOn,
            Supplier = expense.Supplier,
            Note = expense.Note,
            RecordedByName = expense.RecordedByUser != null ? expense.RecordedByUser.Email : null,
            CreatedAt = expense.CreatedAt,
        };

    private static readonly Func<ToolExpense, ToolExpenseDto> Compiled = Projection.Compile();

    public static ToolExpenseDto ToDto(ToolExpense expense) => Compiled(expense);
}

public class GeneralExpenseDto
{
    public Guid Id { get; init; }

    public GeneralExpenseCategory Category { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public Guid? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public string? Supplier { get; init; }

    public string? Note { get; init; }

    public string? RecordedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class GeneralExpenseSummaryDto
{
    public int Count { get; init; }

    public decimal TotalAmount { get; init; }
}

/// <summary>How a <see cref="GeneralExpense"/> becomes a <see cref="GeneralExpenseDto"/>.</summary>
public static class GeneralExpenseMapping
{
    public static readonly Expression<Func<GeneralExpense, GeneralExpenseDto>> Projection =
        expense => new GeneralExpenseDto
        {
            Id = expense.Id,
            Category = expense.Category,
            Amount = expense.Amount,
            OccurredOn = expense.OccurredOn,
            ProjectId = expense.ProjectId,
            ProjectName = expense.Project != null ? expense.Project.Name : null,
            EmployeeId = expense.EmployeeId,
            EmployeeName = expense.Employee != null
                ? expense.Employee.FirstName + " " + expense.Employee.LastName
                : null,
            Supplier = expense.Supplier,
            Note = expense.Note,
            RecordedByName = expense.RecordedByUser != null ? expense.RecordedByUser.Email : null,
            CreatedAt = expense.CreatedAt,
        };

    private static readonly Func<GeneralExpense, GeneralExpenseDto> Compiled = Projection.Compile();

    public static GeneralExpenseDto ToDto(GeneralExpense expense) => Compiled(expense);
}

public class AccommodationRateDto
{
    public Guid Id { get; init; }

    public Guid AccommodationId { get; init; }

    public string AccommodationAddress { get; init; } = null!;

    public decimal MonthlyAmount { get; init; }

    public string? Provider { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public string? Note { get; init; }

    public string? SetByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class AccommodationRateSummaryDto
{
    public int Count { get; init; }

    public decimal TotalMonthlyAmount { get; init; }
}

/// <summary>How an <see cref="AccommodationRate"/> becomes an <see cref="AccommodationRateDto"/>.</summary>
public static class AccommodationRateMapping
{
    public static readonly Expression<Func<AccommodationRate, AccommodationRateDto>> Projection = rate =>
        new AccommodationRateDto
        {
            Id = rate.Id,
            AccommodationId = rate.AccommodationId,
            AccommodationAddress = rate.Accommodation.Address,
            MonthlyAmount = rate.MonthlyAmount,
            Provider = rate.Provider,
            StartDate = rate.StartDate,
            EndDate = rate.EndDate,
            Note = rate.Note,
            SetByName = rate.SetByUser != null ? rate.SetByUser.Email : null,
            CreatedAt = rate.CreatedAt,
        };

    private static readonly Func<AccommodationRate, AccommodationRateDto> Compiled = Projection.Compile();

    public static AccommodationRateDto ToDto(AccommodationRate rate) => Compiled(rate);
}
