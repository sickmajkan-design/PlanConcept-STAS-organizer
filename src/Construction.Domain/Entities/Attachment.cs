using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A file attached to exactly one record: an employee's certificate, a
/// project's drawing, a vehicle's insurance, a tool's calibration sheet.
/// </summary>
/// <remarks>
/// The owner is four nullable foreign keys with a check constraint allowing
/// exactly one, rather than the usual pair of `OwnerType` + `OwnerId`. The
/// discriminator pair is shorter to write and gives up everything the database
/// is for: no foreign key, so an attachment can outlive its owner and point at
/// nothing; no cascade, so deleting an employee leaves their medical records
/// behind. Here, removing an employee removes their documents with them, which
/// is also what a data-erasure request needs.
///
/// A fifth owner type means a fifth column and an updated constraint. That is
/// a migration, which is the point — adding one should be a decision, not a
/// value someone passes in. <see cref="WorkItem"/> was the fifth, so a defect
/// photograph disappears with the defect rather than outliving it.
/// </remarks>
public class Attachment : BaseEntity, ISoftDeletable
{
    /// <summary>Name as uploaded, shown to people. Never used as a path.</summary>
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    /// <summary>
    /// Where <see cref="Construction.Application.Common.Interfaces.IFileStorage"/>
    /// put the bytes. Generated, never taken from the request.
    /// </summary>
    public string StorageKey { get; set; } = null!;

    public AttachmentCategory Category { get; set; } = AttachmentCategory.Other;

    public string? Description { get; set; }

    /// <summary>
    /// When the document stops being valid. Null for anything that does not
    /// lapse, such as a photograph.
    /// </summary>
    public DateOnly? ExpiresAt { get; set; }

    /// <summary>
    /// When an expiry reminder was pushed, so it goes out once rather than
    /// every day until the document is replaced.
    /// </summary>
    public DateTime? ExpiryReminderSentAt { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public Guid? ToolId { get; set; }

    public Tool? Tool { get; set; }

    public Guid? WorkItemId { get; set; }

    public WorkItem? WorkItem { get; set; }

    public Guid? UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    /// <summary>True once the document's validity has run out.</summary>
    public bool IsExpiredOn(DateOnly today) =>
        ExpiresAt is { } expiry && expiry < today;
}
