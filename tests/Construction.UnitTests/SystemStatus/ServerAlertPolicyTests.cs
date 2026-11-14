using Construction.Application.Features.SystemStatus;

namespace Construction.UnitTests.SystemStatus;

public class ServerAlertPolicyTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_notice_never_sent_may_go_out()
    {
        Assert.True(ServerAlertPolicy.CooldownPassed(null, Now));
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    [InlineData(30, true)]
    public void The_same_problem_is_repeated_only_after_six_hours(int hoursAgo, bool expected)
    {
        Assert.Equal(expected, ServerAlertPolicy.CooldownPassed(Now.AddHours(-hoursAgo), Now));
    }

    [Fact]
    public void Every_kind_is_known_once()
    {
        Assert.Equal(ServerAlertKeys.All.Length, ServerAlertKeys.All.Distinct().Count());
        Assert.All(ServerAlertKeys.All, key => Assert.True(ServerAlertKeys.IsKnown(key)));
        Assert.False(ServerAlertKeys.IsKnown("nonsense"));
    }

    [Fact]
    public void Disk_and_outage_are_announced_everywhere_and_the_restore_rehearsal_nowhere_until_chosen()
    {
        Assert.Equal((true, true), ServerAlertKeys.Default(ServerAlertKeys.Disk));
        Assert.Equal((true, true), ServerAlertKeys.Default(ServerAlertKeys.Outage));
        Assert.Equal((false, false), ServerAlertKeys.Default(ServerAlertKeys.RestoreNotChecked));
    }
}
