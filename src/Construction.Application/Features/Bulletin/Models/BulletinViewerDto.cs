using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Bulletin.Models;

/// <summary>One row of the "who has seen this" roster on one bulletin post.</summary>
public class BulletinViewerDto
{
    public Guid UserId { get; init; }

    public string UserEmail { get; init; } = null!;

    public DateTime ViewedAt { get; init; }
}

public static class BulletinViewerMapping
{
    public static readonly Expression<Func<BulletinView, BulletinViewerDto>> Projection =
        view => new BulletinViewerDto
        {
            UserId = view.UserId,
            UserEmail = view.User.Email,
            ViewedAt = view.ViewedAt,
        };
}
