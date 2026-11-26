using Construction.Domain.Common;

namespace Construction.Domain.Entities;

public enum EquipmentKind
{
    Vehicle = 0,
    Tool = 1,
}

/// <summary>
/// Who held a vehicle or tool, and where it stood, over a stretch of days. The present state of an item still lives on
/// the item itself (<c>AssignedEmployeeId</c>, <c>AssignedProjectId</c>); this is its history, written by the database
/// interceptor whenever that state changes, so the planning screen can show an earlier day. Holder and site both empty
/// means "in the store", which is a state too: the rows of one item never leave a gap.
/// </summary>
public class EquipmentHold : BaseEntity
{
    public EquipmentKind Kind { get; set; }

    /// <summary>The vehicle or tool. No relation: the history outlives the item.</summary>
    public Guid ItemId { get; set; }

    public Guid? EmployeeId { get; set; }

    public Guid? ProjectId { get; set; }

    public DateOnly From { get; set; }

    /// <summary>The last day of the stretch, inclusive. Null: it is still going on.</summary>
    public DateOnly? To { get; set; }
}

/// <summary>
/// A change of holder that a planner decided on for a later day ("the car goes to Ivo on Friday"). When the day comes the
/// background service applies it to the item, and the history is written like any other change.
/// </summary>
public class PlannedEquipmentChange : BaseEntity
{
    public EquipmentKind Kind { get; set; }

    public Guid ItemId { get; set; }

    /// <summary>The new holder. With no site either, the item goes to the store.</summary>
    public Guid? EmployeeId { get; set; }

    public Guid? ProjectId { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
