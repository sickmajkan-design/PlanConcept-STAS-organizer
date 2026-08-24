using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Something that has to be done, or something wrong that has to be put right.
/// </summary>
/// <remarks>
/// Named <c>WorkItem</c> rather than <c>Task</c> because the latter collides
/// with <see cref="System.Threading.Tasks.Task"/> in every async method in the
/// codebase, and it also covers both kinds honestly.
/// </remarks>
public class WorkItem : BaseEntity, ISoftDeletable, IAuditable
{
    public WorkItemKind Kind { get; set; } = WorkItemKind.Task;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Required for a defect — a defect exists somewhere — and optional for a
    /// task, which may be office work. Enforced by a check constraint.
    /// </summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>Null while nobody has picked it up.</summary>
    public Guid? AssignedEmployeeId { get; set; }

    public Employee? AssignedEmployee { get; set; }

    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Normal;

    public WorkItemStatus Status { get; set; } = WorkItemStatus.Open;

    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// The assignee must confirm they saw this before they can act on
    /// anything else in the app — for work urgent or hazardous enough that it
    /// cannot just sit unread in the inbox.
    /// </summary>
    public bool RequiresAcknowledgment { get; set; }

    /// <summary>
    /// Where on site the defect is, when the phone had a fix. A site is
    /// hundreds of metres across and "crack in the wall" does not locate it.
    /// </summary>
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public User? ResolvedByUser { get; set; }

    /// <summary>
    /// When the deadline reminder went out, so it goes once rather than every
    /// morning until somebody deals with it.
    /// </summary>
    public DateTime? DueReminderSentAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    /// <summary>Nothing more will happen to it.</summary>
    public bool IsFinished =>
        Status is WorkItemStatus.Closed or WorkItemStatus.Cancelled;

    /// <summary>Past its deadline and still not done.</summary>
    public bool IsOverdueOn(DateOnly today) =>
        !IsFinished && DueDate is { } due && due < today;
}
