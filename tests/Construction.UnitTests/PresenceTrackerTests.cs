using Construction.API.Services;
using Construction.Domain.Enums;

namespace Construction.UnitTests;

public class PresenceTrackerTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_recently_seen_account_is_online_and_an_old_one_is_not()
    {
        var tracker = new PresenceTracker();
        var recent = Guid.NewGuid();
        var stale = Guid.NewGuid();

        tracker.Touch(recent, UserRole.Worker, "app", null, Now);
        tracker.Touch(stale, UserRole.Worker, "app", null, Now.AddMinutes(-10));

        var online = tracker.Online(Now.AddMinutes(-2));

        Assert.Single(online);
        Assert.Equal(recent, online[0].UserId);
    }

    [Fact]
    public void A_SuperAdmin_is_never_tracked()
    {
        var tracker = new PresenceTracker();

        tracker.Touch(Guid.NewGuid(), UserRole.SuperAdmin, "web", "/home", Now);

        Assert.Empty(tracker.Online(Now.AddMinutes(-2)));
        Assert.Empty(tracker.DrainChanged());
    }

    [Fact]
    public void An_ordinary_request_does_not_wipe_the_screen_a_heartbeat_reported()
    {
        var tracker = new PresenceTracker();
        var id = Guid.NewGuid();

        tracker.Touch(id, UserRole.Admin, "web", "/projects", Now);
        tracker.Touch(id, UserRole.Admin, "web", null, Now.AddSeconds(5));

        Assert.Equal("/projects", tracker.Online(Now.AddMinutes(-2))[0].Screen);
    }

    [Fact]
    public void Draining_returns_each_change_once()
    {
        var tracker = new PresenceTracker();
        tracker.Touch(Guid.NewGuid(), UserRole.Worker, "app", null, Now);

        Assert.Single(tracker.DrainChanged());
        Assert.Empty(tracker.DrainChanged());
    }

    [Fact]
    public void Evict_forgets_accounts_not_seen_since_the_cutoff()
    {
        var tracker = new PresenceTracker();
        tracker.Touch(Guid.NewGuid(), UserRole.Worker, "app", null, Now.AddHours(-2));

        tracker.Evict(Now.AddHours(-1));

        Assert.Empty(tracker.Online(DateTime.MinValue));
    }
}
