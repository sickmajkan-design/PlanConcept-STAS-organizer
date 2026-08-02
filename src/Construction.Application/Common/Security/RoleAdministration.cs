using Construction.Application.Common.Exceptions;
using Construction.Domain.Enums;

namespace Construction.Application.Common.Security;

/// <summary>
/// Who may administer whom.
///
/// <para>
/// Role-based policies on the controller decide who reaches the user-management
/// endpoints at all. They cannot express the rule that matters once inside:
/// an administrator must not be able to grant access they do not themselves
/// hold, or take it away from someone senior to them. Without this, any Admin
/// could mint a second Admin — or a Super Admin — and privilege escalation is
/// one request away.
/// </para>
///
/// <para>
/// <see cref="UserRole"/> is ordered by seniority (SuperAdmin = 1 … Worker = 5),
/// so the numeric value is the rank and a smaller value outranks a larger one.
/// </para>
/// </summary>
public static class RoleAdministration
{
    /// <summary>
    /// Whether <paramref name="caller"/> may act on an account holding
    /// <paramref name="target"/>.
    ///
    /// <para>
    /// Everyone may act strictly below themselves. A Super Admin is the
    /// exception and may act on peers, because otherwise a compromised or
    /// departing Super Admin could never be removed by anyone.
    /// </para>
    /// </summary>
    public static bool CanManage(UserRole caller, UserRole target) =>
        caller == UserRole.SuperAdmin || (int)caller < (int)target;

    /// <summary>
    /// Whether <paramref name="caller"/> may grant <paramref name="role"/>.
    /// Same rule as <see cref="CanManage"/>: granting a role you could not
    /// then administer would hand out access you do not hold.
    /// </summary>
    public static bool CanAssign(UserRole caller, UserRole role) => CanManage(caller, role);

    /// <summary>Throws unless the caller outranks the target account.</summary>
    public static void EnsureCanManage(UserRole caller, UserRole target)
    {
        if (!CanManage(caller, target))
        {
            throw new ForbiddenAccessException(
                $"A {caller} may not administer a {target} account.");
        }
    }

    /// <summary>Throws unless the caller may hand out the role.</summary>
    public static void EnsureCanAssign(UserRole caller, UserRole role)
    {
        if (!CanAssign(caller, role))
        {
            throw new ForbiddenAccessException(
                $"A {caller} may not grant the {role} role.");
        }
    }
}
