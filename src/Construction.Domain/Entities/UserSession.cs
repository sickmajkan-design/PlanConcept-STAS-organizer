using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One successful sign-in: who, when, from where, and when they were last seen.
/// </summary>
/// <remarks>
/// One row per sign-in, never per request or heartbeat — the live "who is online"
/// state is held in memory, and <see cref="LastSeenAt"/> is only refreshed in the
/// background every few minutes. That keeps the table to a handful of rows per
/// person per day, and old rows are purged after <c>Retention:UserSessionDays</c>.
/// </remarks>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime LastSeenAt { get; set; }
}
