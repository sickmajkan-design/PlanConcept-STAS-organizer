using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>One employee's membership in a <see cref="NotificationGroup"/>.</summary>
public class NotificationGroupMember : BaseEntity
{
    public Guid NotificationGroupId { get; set; }

    public NotificationGroup NotificationGroup { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;
}
