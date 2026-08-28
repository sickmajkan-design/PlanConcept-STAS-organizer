using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Bulletin.Models;

public class BulletinPostDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;

    public string CreatedByName { get; init; } = null!;

    public DateTime CreatedAt { get; init; }

    public int ViewCount { get; init; }

    /// <summary>Whether the caller has already viewed this post.</summary>
    public bool Viewed { get; init; }
}

/// <summary>
/// Builds a <see cref="BulletinPostDto"/> for one specific caller — unlike
/// every other mapping in this codebase, this one is not caller-independent,
/// because <see cref="BulletinPostDto.Viewed"/> only means something relative
/// to whoever is asking.
/// </summary>
public static class BulletinPostMapping
{
    public static Expression<Func<BulletinPost, BulletinPostDto>> ProjectionFor(Guid currentUserId) =>
        post => new BulletinPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            CreatedByName = post.CreatedByUser.Email,
            CreatedAt = post.CreatedAt,
            ViewCount = post.Views.Count,
            Viewed = post.Views.Any(v => v.UserId == currentUserId),
        };
}
