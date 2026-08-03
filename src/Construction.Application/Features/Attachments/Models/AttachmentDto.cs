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
            .ForMember(d => d.OwnerType, opt => opt.MapFrom(s =>
                s.EmployeeId != null ? AttachmentOwnerType.Employee
                : s.ProjectId != null ? AttachmentOwnerType.Project
                : s.VehicleId != null ? AttachmentOwnerType.Vehicle
                : AttachmentOwnerType.Tool))
            .ForMember(d => d.OwnerId, opt => opt.MapFrom(s =>
                s.EmployeeId != null ? s.EmployeeId.Value
                : s.ProjectId != null ? s.ProjectId.Value
                : s.VehicleId != null ? s.VehicleId.Value
                : s.ToolId!.Value))
            .ForMember(d => d.OwnerName, opt => opt.MapFrom(s =>
                s.Employee != null ? s.Employee.FirstName + " " + s.Employee.LastName
                : s.Project != null ? s.Project.Name
                : s.Vehicle != null ? s.Vehicle.Brand + " " + s.Vehicle.Model
                : s.Tool != null ? s.Tool.Name
                : null))
            .ForMember(d => d.UploadedByName, opt => opt.MapFrom(s =>
                s.UploadedByUser != null ? s.UploadedByUser.Email : null));
    }
}
