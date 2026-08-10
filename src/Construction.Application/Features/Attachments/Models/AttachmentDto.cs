using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Attachments.Models;

public class AttachmentDto
{
    public Guid Id { get; init; }

    public string FileName { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public long SizeBytes { get; init; }

    public AttachmentCategory Category { get; init; }

    public string? Description { get; init; }

    public DateOnly? ExpiresAt { get; init; }

    public AttachmentOwnerType OwnerType { get; init; }

    public Guid OwnerId { get; init; }

    /// <summary>Name of the record it hangs off, so a list needs no second call.</summary>
    public string? OwnerName { get; init; }

    public string? UploadedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Attachment"/> becomes an <see cref="AttachmentDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class AttachmentMapping
{
    public static readonly Expression<Func<Attachment, AttachmentDto>> Projection = attachment =>
        new AttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
            Category = attachment.Category,
            Description = attachment.Description,
            ExpiresAt = attachment.ExpiresAt,
            // Spelled out as a conditional chain rather than through
            // AttachmentOwner.Of, because this has to become SQL: a method call
            // cannot be translated, and loading every row to ask it in memory is
            // what a list endpoint must not do.
            // Every branch ends on an explicit column rather than falling
            // through to the last one. The chain used to end at Tool, so a row
            // owned by anything the chain had not been taught reported itself
            // as a tool with a null id — which threw on projection, and would
            // have been worse if it had not.
            OwnerType = attachment.EmployeeId != null ? AttachmentOwnerType.Employee
                : attachment.ProjectId != null ? AttachmentOwnerType.Project
                : attachment.VehicleId != null ? AttachmentOwnerType.Vehicle
                : attachment.ToolId != null ? AttachmentOwnerType.Tool
                : AttachmentOwnerType.WorkItem,
            OwnerId = attachment.EmployeeId != null ? attachment.EmployeeId.Value
                : attachment.ProjectId != null ? attachment.ProjectId.Value
                : attachment.VehicleId != null ? attachment.VehicleId.Value
                : attachment.ToolId != null ? attachment.ToolId.Value
                : attachment.WorkItemId!.Value,
            OwnerName = attachment.Employee != null
                ? attachment.Employee.FirstName + " " + attachment.Employee.LastName
                : attachment.Project != null ? attachment.Project.Name
                : attachment.Vehicle != null ? attachment.Vehicle.Brand + " " + attachment.Vehicle.Model
                : attachment.Tool != null ? attachment.Tool.Name
                : attachment.WorkItem != null ? attachment.WorkItem.Title
                : null,
            UploadedByName = attachment.UploadedByUser != null ? attachment.UploadedByUser.Email : null,
            CreatedAt = attachment.CreatedAt,
        };

    private static readonly Func<Attachment, AttachmentDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static AttachmentDto ToDto(Attachment attachment) => Compiled(attachment);
}
