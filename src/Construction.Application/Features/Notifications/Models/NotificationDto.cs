using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Notifications.Models;

public class NotificationDto
{
    public Guid Id { get; init; }

    public string Type { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;

    /// <summary>Optional JSON payload with deep-link data (entity ids etc.).</summary>
    public string? DataJson { get; init; }

    public bool IsRead { get; init; }

    public DateTime? ReadAt { get; init; }

    public bool RequiresAcknowledgment { get; init; }

    public DateTime? AcknowledgedAt { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// How a <see cref="Notification"/> becomes an <see cref="NotificationDto"/>.
/// </summary>
/// <remarks>
/// One expression, used two ways: EF Core translates <see cref="Projection"/>
/// into the SELECT list of a query, and <see cref="ToDto"/> runs the same
/// expression compiled, in memory. See <c>EmployeeMapping</c> for why this
/// replaced AutoMapper.
/// </remarks>
public static class NotificationMapping
{
    public static readonly Expression<Func<Notification, NotificationDto>> Projection = notification =>
        new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type.ToString(),
            Title = notification.Title,
            Body = notification.Body,
            DataJson = notification.DataJson,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            RequiresAcknowledgment = notification.RequiresAcknowledgment,
            AcknowledgedAt = notification.AcknowledgedAt,
            CreatedAt = notification.CreatedAt,
        };

    private static readonly Func<Notification, NotificationDto> Compiled = Projection.Compile();

    /// <summary>Maps a record already in memory.</summary>
    public static NotificationDto ToDto(Notification notification) => Compiled(notification);
}
