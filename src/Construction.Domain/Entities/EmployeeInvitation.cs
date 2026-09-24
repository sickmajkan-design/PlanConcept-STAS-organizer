using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A one-time link that lets an employee create their own sign-in account,
/// so an administrator never has to invent, type and pass on a password.
/// </summary>
/// <remarks>
/// Only a hash of the token is stored: the link is shown to the administrator
/// once, and a database read must not be enough to take over an invitation.
/// A newer invitation for the same employee replaces any earlier open one, so
/// there is only ever one link that works per person.
/// </remarks>
public class EmployeeInvitation : BaseEntity
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    /// <summary>SHA-256 of the token, hex-encoded.</summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>The role the account is created with.</summary>
    public UserRole Role { get; set; } = UserRole.Worker;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the invitation is accepted or replaced by a newer one.</summary>
    public DateTime? UsedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public bool IsOpen(DateTime utcNow) => UsedAt is null && ExpiresAt > utcNow;
}
