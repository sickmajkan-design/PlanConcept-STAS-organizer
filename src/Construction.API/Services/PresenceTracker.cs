using System.Collections.Concurrent;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;

namespace Construction.API.Services;

/// <inheritdoc />
/// <remarks>
/// A single API instance holds the truth. If a second one is ever added this is
/// the one piece to move to a shared store (Redis); everything that reads it
/// goes through <see cref="IPresenceTracker"/>.
/// </remarks>
public sealed class PresenceTracker : IPresenceTracker
{
    private sealed class Slot
    {
        public DateTime LastSeenAt;
        public string? Screen;
        public string Client = "web";
        public bool Changed;
    }

    private readonly ConcurrentDictionary<Guid, Slot> _slots = new();

    public void Touch(Guid userId, UserRole role, string client, string? screen, DateTime utcNow)
    {
        if (role == UserRole.SuperAdmin)
        {
            return;
        }

        var slot = _slots.GetOrAdd(userId, _ => new Slot());

        lock (slot)
        {
            slot.LastSeenAt = utcNow;
            slot.Client = client;
            slot.Changed = true;

            // Only a heartbeat names a screen; an ordinary API call must not wipe it.
            if (screen is not null)
            {
                slot.Screen = screen;
            }
        }
    }

    public IReadOnlyList<PresenceEntry> Online(DateTime since) =>
        Snapshot(markDrained: false)
            .Where(e => e.LastSeenAt >= since)
            .OrderByDescending(e => e.LastSeenAt)
            .ToList();

    public IReadOnlyList<PresenceEntry> DrainChanged() => Snapshot(markDrained: true);

    public void Evict(DateTime before)
    {
        foreach (var (id, slot) in _slots)
        {
            lock (slot)
            {
                if (slot.LastSeenAt < before)
                {
                    _slots.TryRemove(id, out _);
                }
            }
        }
    }

    private List<PresenceEntry> Snapshot(bool markDrained)
    {
        var result = new List<PresenceEntry>();

        foreach (var (id, slot) in _slots)
        {
            lock (slot)
            {
                if (markDrained)
                {
                    if (!slot.Changed) continue;
                    slot.Changed = false;
                }

                result.Add(new PresenceEntry(id, slot.LastSeenAt, slot.Screen, slot.Client));
            }
        }

        return result;
    }
}
