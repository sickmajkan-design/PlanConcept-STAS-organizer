namespace Construction.Domain.Enums;

public enum WorkItemPriority
{
    Low = 1,
    Normal = 2,
    High = 3,

    /// <summary>Work stops until this is dealt with.</summary>
    Urgent = 4
}
