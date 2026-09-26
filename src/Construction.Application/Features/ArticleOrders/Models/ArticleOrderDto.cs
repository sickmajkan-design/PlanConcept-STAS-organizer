using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.ArticleOrders.Models;

public class ArticleOrderItemDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public decimal Quantity { get; init; }

    public string? Unit { get; init; }

    public string? Note { get; init; }
}

public class ArticleOrderDto
{
    public Guid Id { get; init; }

    public ArticleOrderStatus Status { get; init; }

    public bool Urgent { get; init; }

    public string? Note { get; init; }

    public string? ReviewNote { get; init; }

    public Guid RequestedByUserId { get; init; }

    /// <summary>The person's name, or their sign-in when no employee record is behind the account.</summary>
    public string RequestedByName { get; init; } = null!;

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public string? HandledByName { get; init; }

    public DateTime? OrderedAt { get; init; }

    public DateTime? ShippedAt { get; init; }

    public DateTime? DeliveredAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<ArticleOrderItemDto> Items { get; init; } = [];
}

/// <summary>One expression, used as the SELECT list. See <c>AbsenceMapping</c>.</summary>
public static class ArticleOrderMapping
{
    public static readonly Expression<Func<ArticleOrder, ArticleOrderDto>> Projection = order =>
        new ArticleOrderDto
        {
            Id = order.Id,
            Status = order.Status,
            Urgent = order.Urgent,
            Note = order.Note,
            ReviewNote = order.ReviewNote,
            RequestedByUserId = order.RequestedByUserId,
            RequestedByName = order.Employee != null
                ? order.Employee.FirstName + " " + order.Employee.LastName
                : order.RequestedByUser.Email,
            ProjectId = order.ProjectId,
            ProjectName = order.Project != null ? order.Project.Name : null,
            HandledByName = order.HandledByUser != null ? order.HandledByUser.Email : null,
            OrderedAt = order.OrderedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CreatedAt = order.CreatedAt,
            Items = order.Items
                .OrderBy(i => i.CreatedAt).ThenBy(i => i.Id)
                .Select(i => new ArticleOrderItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Note = i.Note,
                })
                .ToList(),
        };
}
