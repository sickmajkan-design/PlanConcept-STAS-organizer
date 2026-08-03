using Construction.Application.Features.Absences;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Security;

public class AbsenceRulesTests
{
    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    public void A_supervisor_may_grant_leave(UserRole role)
    {
        Assert.True(AbsenceRules.CanReview(role));
    }

    [Fact]
    public void A_worker_may_not_grant_leave()
    {
        Assert.False(AbsenceRules.CanReview(UserRole.Worker));
    }

    [Fact]
    public void A_caller_with_no_role_may_not_grant_leave()
    {
        // A token missing its role claim must not fall through to permission.
        Assert.False(AbsenceRules.CanReview(null));
    }

    [Fact]
    public void Booking_for_somebody_else_needs_the_same_standing_as_granting()
    {
        // Recording leave for another person and approving it are the same
        // authority: both let one person decide another's schedule.
        foreach (var role in Enum.GetValues<UserRole>())
        {
            Assert.Equal(AbsenceRules.CanReview(role), AbsenceRules.CanRequestForOthers(role));
        }

        Assert.False(AbsenceRules.CanRequestForOthers(null));
    }

    [Fact]
    public void A_worker_sees_only_their_own_absences()
    {
        Assert.True(AbsenceRules.IsRestrictedToOwnAbsences(UserRole.Worker));
        Assert.True(AbsenceRules.IsRestrictedToOwnAbsences(null));
    }

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    public void A_supervisor_sees_everybody(UserRole role)
    {
        Assert.False(AbsenceRules.IsRestrictedToOwnAbsences(role));
    }

    [Fact]
    public void The_windows_are_ordered_the_way_the_rules_read()
    {
        // Leave may be booked further ahead than it may be backdated, and no
        // single absence may run longer than a year's booking horizon. If
        // these ever cross, the validator's messages start contradicting each
        // other rather than failing loudly.
        Assert.True(AbsenceRules.MaxLeadDays > AbsenceRules.MaxBackdatingDays);
        Assert.True(AbsenceRules.MaxDays < AbsenceRules.MaxLeadDays);
    }
}
