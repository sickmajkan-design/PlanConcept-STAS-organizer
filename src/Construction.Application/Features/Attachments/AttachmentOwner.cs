using Construction.Domain.Entities;

namespace Construction.Application.Features.Attachments;

/// <summary>Which kind of record a file hangs off.</summary>
public enum AttachmentOwnerType
{
    Employee = 1,
    Project = 2,
    Vehicle = 3,
    Tool = 4
}

/// <summary>
/// Translates between the owner as the API talks about it — a type and an id —
/// and the four nullable columns the table actually uses.
/// </summary>
/// <remarks>
/// The pair is a convenient shape for a request and a poor one for a database.
/// This is the one place that knows both, so the columns stay real foreign
/// keys with real cascades while callers still get to say "employee X".
/// </remarks>
public static class AttachmentOwner
{
    /// <summary>Points <paramref name="attachment"/> at the given owner.</summary>
    public static void Apply(Attachment attachment, AttachmentOwnerType type, Guid id)
    {
        attachment.EmployeeId = type == AttachmentOwnerType.Employee ? id : null;
        attachment.ProjectId = type == AttachmentOwnerType.Project ? id : null;
        attachment.VehicleId = type == AttachmentOwnerType.Vehicle ? id : null;
        attachment.ToolId = type == AttachmentOwnerType.Tool ? id : null;
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
        _ => "other"
    };
}
