using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Security;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Security;

/// <summary>
/// The whole privilege-escalation surface of user management is this one rule,
/// so it is worth stating exhaustively rather than by example.
/// </summary>
public class RoleAdministrationTests
{
    public static TheoryData<UserRole, UserRole, bool> ManagementMatrix()
    {
        var roles = Enum.GetValues<UserRole>();
        var data = new TheoryData<UserRole, UserRole, bool>();

        foreach (var caller in roles)
        {
            foreach (var target in roles)
            {
                // Everyone may act strictly below themselves; a Super Admin
                // may also act on peers, or a compromised Super Admin could
                // never be removed.
                var expected = caller == UserRole.SuperAdmin || (int)caller < (int)target;
                data.Add(caller, target, expected);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ManagementMatrix))]
    public void Every_pair_of_roles_resolves_the_same_way(UserRole caller, UserRole target, bool expected)
    {
        Assert.Equal(expected, RoleAdministration.CanManage(caller, target));
        Assert.Equal(expected, RoleAdministration.CanAssign(caller, target));
    }

    [Theory]
    [InlineData(UserRole.Admin, UserRole.Admin)]
    [InlineData(UserRole.Admin, UserRole.SuperAdmin)]
    [InlineData(UserRole.ProjectManager, UserRole.Admin)]
    [InlineData(UserRole.Worker, UserRole.Worker)]
    public void Nobody_may_administer_their_own_level_or_above(UserRole caller, UserRole target)
    {
        Assert.False(RoleAdministration.CanManage(caller, target));
        Assert.Throws<ForbiddenAccessException>(
            () => RoleAdministration.EnsureCanManage(caller, target));
    }

    [Fact]
    public void An_admin_cannot_grant_admin_which_is_what_stops_self_replication()
    {
        // Otherwise one compromised Admin account becomes any number of them.
        Assert.False(RoleAdministration.CanAssign(UserRole.Admin, UserRole.Admin));
        Assert.Throws<ForbiddenAccessException>(
            () => RoleAdministration.EnsureCanAssign(UserRole.Admin, UserRole.Admin));
    }

    [Fact]
    public void A_super_admin_may_administer_every_role_including_peers()
    {
        foreach (var target in Enum.GetValues<UserRole>())
        {
            Assert.True(RoleAdministration.CanManage(UserRole.SuperAdmin, target));
        }
    }

    [Fact]
    public void An_admin_may_administer_everyone_below()
    {
        foreach (var target in new[] { UserRole.ProjectManager, UserRole.Foreman, UserRole.Worker })
        {
            Assert.True(RoleAdministration.CanManage(UserRole.Admin, target));
            RoleAdministration.EnsureCanManage(UserRole.Admin, target);
        }
    }

    [Fact]
    public void The_message_names_both_roles_so_the_refusal_is_actionable()
    {
        var exception = Assert.Throws<ForbiddenAccessException>(
            () => RoleAdministration.EnsureCanAssign(UserRole.Admin, UserRole.SuperAdmin));

        Assert.Contains(nameof(UserRole.Admin), exception.Message);
        Assert.Contains(nameof(UserRole.SuperAdmin), exception.Message);
    }
}
