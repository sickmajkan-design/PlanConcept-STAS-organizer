using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.NotificationGroups.Models;

public class NotificationGroupDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public int MemberCount { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

/// <summary>The list/summary shape plus who is in it, for the edit form.</summary>
public class NotificationGroupDetailDto : NotificationGroupDto
{
    public List<Guid> MemberEmployeeIds { get; init; } = [];
}

public static class NotificationGroupMapping
{
    public static readonly Expression<Func<NotificationGroup, NotificationGroupDto>> Projection = group =>
        new NotificationGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
        };

    public static readonly Expression<Func<NotificationGroup, NotificationGroupDetailDto>> DetailProjection = group =>
        new NotificationGroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            MemberEmployeeIds = group.Members.Select(m => m.EmployeeId).ToList(),
        };
}
