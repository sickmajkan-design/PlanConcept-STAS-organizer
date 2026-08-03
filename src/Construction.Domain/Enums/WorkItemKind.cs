namespace Construction.Domain.Enums;

/// <summary>
/// What a work item is.
/// </summary>
/// <remarks>
/// One table rather than two, because a defect is a task with a place and a
/// photograph attached. Everything else — who it is for, when it is due, what
/// state it is in, who closed it — is identical, and two tables would have
/// meant two of every query, screen and notification for one differing field.
/// </remarks>
public enum WorkItemKind
{
    /// <summary>Something to do.</summary>
    Task = 1,

    /// <summary>Something wrong on site that has to be put right.</summary>
    Defect = 2
}
