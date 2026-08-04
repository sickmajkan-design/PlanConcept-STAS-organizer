using AutoMapper;
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

public class AttachmentDtoMappingProfile : Profile
{
    public AttachmentDtoMappingProfile()
    {
        CreateMap<Attachment, AttachmentDto>()
            // Spelled out as a conditional chain rather than through
            // AttachmentOwner.Of, because this has to become SQL: ProjectTo
            // cannot translate a method call, and loading every row to ask it
            // in memory is what a list endpoint must not do.
            // Every branch ends on an explicit column rather than falling
            // through to the last one. The chain used to end at Tool, so a row
            // owned by anything the chain had not been taught reported itself
            // as a tool with a null id — which threw on projection, and would
            // have been worse if it had not.
            .ForMember(d => d.OwnerType, opt => opt.MapFrom(s =>
                s.EmployeeId != null ? AttachmentOwnerType.Employee
                : s.ProjectId != null ? AttachmentOwnerType.Project
                : s.VehicleId != null ? AttachmentOwnerType.Vehicle
                : s.ToolId != null ? AttachmentOwnerType.Tool
                : AttachmentOwnerType.WorkItem))
            .ForMember(d => d.OwnerId, opt => opt.MapFrom(s =>
                s.EmployeeId != null ? s.EmployeeId.Value
                : s.ProjectId != null ? s.ProjectId.Value
                : s.VehicleId != null ? s.VehicleId.Value
                : s.ToolId != null ? s.ToolId.Value
                : s.WorkItemId!.Value))
            .ForMember(d => d.OwnerName, opt => opt.MapFrom(s =>
                s.Employee != null ? s.Employee.FirstName + " " + s.Employee.LastName
                : s.Project != null ? s.Project.Name
                : s.Vehicle != null ? s.Vehicle.Brand + " " + s.Vehicle.Model
                : s.Tool != null ? s.Tool.Name
                : s.WorkItem != null ? s.WorkItem.Title
                : null))
            .ForMember(d => d.UploadedByName, opt => opt.MapFrom(s =>
                s.UploadedByUser != null ? s.UploadedByUser.Email : null));
    }
}
