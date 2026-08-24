using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A named, admin-defined list of employees, used only to narrow who an
/// announcement reaches. Not a general organisational concept — nothing else
/// in the system reads membership.
/// </summary>
public class NotificationGroup : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<NotificationGroupMember> Members { get; set; } =
        new List<NotificationGroupMember>();
}
