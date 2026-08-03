using AutoMapper;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.WorkItems.Models;

public class WorkItemDto
{
    public Guid Id { get; init; }

    public WorkItemKind Kind { get; init; }

    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public Guid? AssignedEmployeeId { get; init; }

    public string? AssignedEmployeeName { get; init; }

    public WorkItemPriority Priority { get; init; }

    public WorkItemStatus Status { get; init; }

    public DateOnly? DueDate { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public string? CreatedByName { get; init; }

    public string? ResolvedByName { get; init; }

    public DateTime? ResolvedAt { get; init; }

    /// <summary>How many photographs are attached, so a list can show a badge
    /// without a second request per row.</summary>
    public int AttachmentCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public bool IsFinished =>
        Status is WorkItemStatus.Closed or WorkItemStatus.Cancelled;
}

public class WorkItemDtoMappingProfile : Profile
{
    public WorkItemDtoMappingProfile()
    {
        CreateMap<WorkItem, WorkItemDto>()
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s =>
                s.Project != null ? s.Project.Name : null))
            .ForMember(d => d.AssignedEmployeeName, opt => opt.MapFrom(s =>
                s.AssignedEmployee != null
                    ? s.AssignedEmployee.FirstName + " " + s.AssignedEmployee.LastName
                    : null))
            .ForMember(d => d.CreatedByName, opt => opt.MapFrom(s =>
                s.CreatedByUser != null ? s.CreatedByUser.Email : null))
            .ForMember(d => d.ResolvedByName, opt => opt.MapFrom(s =>
                s.ResolvedByUser != null ? s.ResolvedByUser.Email : null))
            // Counted in the same query rather than loading the rows: a board
            // showing fifty items would otherwise make fifty-one round trips.
            .ForMember(d => d.AttachmentCount, opt => opt.MapFrom(s =>
                s.Attachments.Count));
    }
}
