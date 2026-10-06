using Construction.Domain.Enums;

namespace Construction.Application.Common.Interfaces;

/// <summary>One account's live state, as last reported.</summary>
public record PresenceEntry(Guid UserId, DateTime LastSeenAt, string? Screen, string Client);

/// <summary>
/// Who is using the platform right now. Held in memory on purpose: a signal
/// from every open tab every minute would be the busiest table in the database
/// for no lasting value. Only a throttled "last seen" ever reaches the database.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>Records activity. SuperAdmin accounts are deliberately never tracked.</summary>
    void Touch(Guid userId, UserRole role, string client, string? screen, DateTime utcNow);

    /// <summary>Everybody seen at or after <paramref name="since"/>, newest first.</summary>
    IReadOnlyList<PresenceEntry> Online(DateTime since);

    /// <summary>The accounts seen since the last drain, for the background "last seen" write.</summary>
    IReadOnlyList<PresenceEntry> DrainChanged();

    /// <summary>Forgets accounts not seen since <paramref name="before"/>.</summary>
    void Evict(DateTime before);
}
