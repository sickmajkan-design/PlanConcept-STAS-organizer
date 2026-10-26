using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A traffic or parking fine a vehicle has collected, entered by the office. It says what for, how much, by when,
/// who was driving and who bears it. Nothing is charged automatically: a fine the driver bears is only recorded as
/// owed, and how it is collected stays the office's decision.
/// </summary>
public class VehicleFine : BaseEntity, IAuditable
{
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    /// <summary>The day of the offence.</summary>
    public DateOnly OccurredOn { get; set; }

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    /// <summary>The last day it can be paid. Null when the notice gives none.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Who was driving. Null when nobody knows.</summary>
    public Guid? DriverEmployeeId { get; set; }

    public Employee? DriverEmployee { get; set; }

    public VehicleFineBearer BorneBy { get; set; } = VehicleFineBearer.Firm;

    public VehicleFineStatus Status { get; set; } = VehicleFineStatus.Open;

    public DateOnly? PaidOn { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}

/// <summary>
/// "This admin was told about this fine at this stage" (days before the due date), so a daily sweep never tells
/// anyone twice about the same stage of the same fine.
/// </summary>
public class VehicleFineReminder : BaseEntity
{
    public Guid VehicleFineId { get; set; }

    public VehicleFine VehicleFine { get; set; } = null!;

    public int DaysBefore { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTime SentAt { get; set; }
}
