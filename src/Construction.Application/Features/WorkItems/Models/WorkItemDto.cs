using System.Linq.Expressions;
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

/// <summary>
/// How a <see cref="WorkItem"/> becomes an <see cref="WorkItemDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class WorkItemMapping
{
    public static readonly Expression<Func<WorkItem, WorkItemDto>> Projection = item =>
        new WorkItemDto
        {
            Id = item.Id,
            Kind = item.Kind,
            Title = item.Title,
            Description = item.Description,
            ProjectId = item.ProjectId,
            ProjectName = item.Project != null ? item.Project.Name : null,
            AssignedEmployeeId = item.AssignedEmployeeId,
            AssignedEmployeeName = item.AssignedEmployee != null
                ? item.AssignedEmployee.FirstName + " " + item.AssignedEmployee.LastName
                : null,
            Priority = item.Priority,
            Status = item.Status,
            DueDate = item.DueDate,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            CreatedByName = item.CreatedByUser != null ? item.CreatedByUser.Email : null,
            ResolvedByName = item.ResolvedByUser != null ? item.ResolvedByUser.Email : null,
            ResolvedAt = item.ResolvedAt,
            // Counted in the same query rather than loading the rows: a board
            // showing fifty items would otherwise make fifty-one round trips.
            AttachmentCount = item.Attachments.Count,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
        };

    private static readonly Func<WorkItem, WorkItemDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static WorkItemDto ToDto(WorkItem item) => Compiled(item);
}
