using Construction.Domain.Entities;

namespace Construction.Application.Features.Attachments;

/// <summary>Which kind of record a file hangs off.</summary>
public enum AttachmentOwnerType
{
    Employee = 1,
    Project = 2,
    Vehicle = 3,
    Tool = 4,

    /// <summary>A task or a defect. The photograph of the crack.</summary>
    WorkItem = 5,

    /// <summary>The receipt for a tank of fuel, a service, or another vehicle cost.</summary>
    VehicleExpense = 6,

    /// <summary>The delivery note or issue slip behind a stock movement.</summary>
    MaterialMovement = 7,

    /// <summary>A signed contract or annex backing a pay rate.</summary>
    EmployeeRate = 8,

    /// <summary>The payslip or receipt behind a recorded pay entry.</summary>
    FinanceEntry = 9,

    /// <summary>The receipt for a tool's repair, service, or calibration.</summary>
    ToolExpense = 10,

    /// <summary>The rental or lease contract behind a vehicle's rental rate.</summary>
    VehicleRentalRate = 11,

    /// <summary>The receipt or evidence behind a general expense — a damage photo, an invoice.</summary>
    GeneralExpense = 12,

    /// <summary>A photo or document of the apartment itself.</summary>
    Accommodation = 13,

    /// <summary>The lease contract behind an accommodation's rate.</summary>
    AccommodationRate = 14,

    /// <summary>The rental or lease contract behind a tool's rental rate.</summary>
    ToolRentalRate = 15
}

/// <summary>
/// Translates between the owner as the API talks about it — a type and an id —
/// and the five nullable columns the table actually uses.
/// </summary>
/// <remarks>
/// The pair is a convenient shape for a request and a poor one for a database.
/// This is the one place that knows both, so the columns stay real foreign
/// keys with real cascades while callers still get to say "employee X".
/// </remarks>
public static class AttachmentOwner
{
    /// <summary>Points <paramref name="attachment"/> at the given owner.</summary>
    /// <remarks>
    /// Every column is assigned on every call, including the nulls. Setting
    /// only the matching one would leave a previous owner in place on a reused
    /// instance, and the table's check constraint would then reject the row
    /// with nothing to say which of the two was meant.
    /// </remarks>
    public static void Apply(Attachment attachment, AttachmentOwnerType type, Guid id)
    {
        attachment.EmployeeId = type == AttachmentOwnerType.Employee ? id : null;
        attachment.ProjectId = type == AttachmentOwnerType.Project ? id : null;
        attachment.VehicleId = type == AttachmentOwnerType.Vehicle ? id : null;
        attachment.ToolId = type == AttachmentOwnerType.Tool ? id : null;
        attachment.WorkItemId = type == AttachmentOwnerType.WorkItem ? id : null;
        attachment.VehicleExpenseId = type == AttachmentOwnerType.VehicleExpense ? id : null;
        attachment.MaterialMovementId = type == AttachmentOwnerType.MaterialMovement ? id : null;
        attachment.EmployeeRateId = type == AttachmentOwnerType.EmployeeRate ? id : null;
        attachment.FinanceEntryId = type == AttachmentOwnerType.FinanceEntry ? id : null;
        attachment.ToolExpenseId = type == AttachmentOwnerType.ToolExpense ? id : null;
        attachment.VehicleRentalRateId = type == AttachmentOwnerType.VehicleRentalRate ? id : null;
        attachment.GeneralExpenseId = type == AttachmentOwnerType.GeneralExpense ? id : null;
        attachment.AccommodationId = type == AttachmentOwnerType.Accommodation ? id : null;
        attachment.AccommodationRateId = type == AttachmentOwnerType.AccommodationRate ? id : null;
        attachment.ToolRentalRateId = type == AttachmentOwnerType.ToolRentalRate ? id : null;
    }

    /// <summary>Reads the owner back off a stored row.</summary>
    public static (AttachmentOwnerType Type, Guid Id) Of(Attachment attachment)
    {
        if (attachment.EmployeeId is { } employeeId)
        {
            return (AttachmentOwnerType.Employee, employeeId);
        }

        if (attachment.ProjectId is { } projectId)
        {
            return (AttachmentOwnerType.Project, projectId);
        }

        if (attachment.VehicleId is { } vehicleId)
        {
            return (AttachmentOwnerType.Vehicle, vehicleId);
        }

        if (attachment.ToolId is { } toolId)
        {
            return (AttachmentOwnerType.Tool, toolId);
        }

        if (attachment.WorkItemId is { } workItemId)
        {
            return (AttachmentOwnerType.WorkItem, workItemId);
        }

        if (attachment.VehicleExpenseId is { } vehicleExpenseId)
        {
            return (AttachmentOwnerType.VehicleExpense, vehicleExpenseId);
        }

        if (attachment.MaterialMovementId is { } materialMovementId)
        {
            return (AttachmentOwnerType.MaterialMovement, materialMovementId);
        }

        if (attachment.EmployeeRateId is { } employeeRateId)
        {
            return (AttachmentOwnerType.EmployeeRate, employeeRateId);
        }

        if (attachment.FinanceEntryId is { } financeEntryId)
        {
            return (AttachmentOwnerType.FinanceEntry, financeEntryId);
        }

        if (attachment.ToolExpenseId is { } toolExpenseId)
        {
            return (AttachmentOwnerType.ToolExpense, toolExpenseId);
        }

        if (attachment.VehicleRentalRateId is { } vehicleRentalRateId)
        {
            return (AttachmentOwnerType.VehicleRentalRate, vehicleRentalRateId);
        }

        if (attachment.GeneralExpenseId is { } generalExpenseId)
        {
            return (AttachmentOwnerType.GeneralExpense, generalExpenseId);
        }

        if (attachment.AccommodationId is { } accommodationId)
        {
            return (AttachmentOwnerType.Accommodation, accommodationId);
        }

        if (attachment.AccommodationRateId is { } accommodationRateId)
        {
            return (AttachmentOwnerType.AccommodationRate, accommodationRateId);
        }

        if (attachment.ToolRentalRateId is { } toolRentalRateId)
        {
            return (AttachmentOwnerType.ToolRentalRate, toolRentalRateId);
        }

        // The table's check constraint makes this unreachable; if it is ever
        // reached, something has bypassed the database and guessing an owner
        // would hide it.
        throw new InvalidOperationException(
            $"Attachment {attachment.Id} has no owner.");
    }

    /// <summary>The prefix a stored object is filed under.</summary>
    public static string PathSegment(AttachmentOwnerType type) => type switch
    {
        AttachmentOwnerType.Employee => "employees",
        AttachmentOwnerType.Project => "projects",
        AttachmentOwnerType.Vehicle => "vehicles",
        AttachmentOwnerType.Tool => "tools",
        AttachmentOwnerType.WorkItem => "work-items",
        AttachmentOwnerType.VehicleExpense => "vehicle-expenses",
        AttachmentOwnerType.MaterialMovement => "material-movements",
        AttachmentOwnerType.EmployeeRate => "employee-rates",
        AttachmentOwnerType.FinanceEntry => "finance-entries",
        AttachmentOwnerType.ToolExpense => "tool-expenses",
        AttachmentOwnerType.VehicleRentalRate => "vehicle-rental-rates",
        AttachmentOwnerType.GeneralExpense => "general-expenses",
        AttachmentOwnerType.Accommodation => "accommodations",
        AttachmentOwnerType.AccommodationRate => "accommodation-rates",
        AttachmentOwnerType.ToolRentalRate => "tool-rental-rates",
        _ => "other"
    };
}
