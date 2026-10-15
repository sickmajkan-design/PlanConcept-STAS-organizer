using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// The office asking a person for a document that is missing — a medical check, a visa, a driving
/// licence — and the person answering it from the phone.
/// </summary>
/// <remarks>
/// The document itself is an <see cref="Attachment"/> on the person's own record; this row only
/// tracks the ask. A returned request keeps the files that were sent (they are named by the
/// <see cref="DocumentRequestEvent"/> that recorded the sending), so what was refused and why stays
/// on the record. Removing the employee removes their requests.
/// </remarks>
public class DocumentRequest : BaseEntity, IAuditable
{
    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public User RequestedByUser { get; set; } = null!;

    /// <summary>What is asked for. The category the document gets when it is accepted.</summary>
    public AttachmentCategory Category { get; set; }

    /// <summary>Shown to the person, so it must carry nothing they should not see on a lock screen.</summary>
    public string Title { get; set; } = null!;

    public string? Note { get; set; }

    public DateOnly? DueDate { get; set; }

    public DocumentRequestStatus Status { get; set; } = DocumentRequestStatus.Open;

    /// <summary>How many times the person has sent something for this request.</summary>
    public int Attempts { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Why it was sent back. Shown to the person.</summary>
    public string? ReturnReason { get; set; }

    public ICollection<DocumentRequestEvent> Events { get; set; } = new List<DocumentRequestEvent>();
}

/// <summary>One entry in a request's history.</summary>
public class DocumentRequestEvent : BaseEntity
{
    public Guid DocumentRequestId { get; set; }

    public DocumentRequest DocumentRequest { get; set; } = null!;

    public DocumentRequestEventType Type { get; set; }

    public Guid? ActorUserId { get; set; }

    /// <summary>The reason, for a request sent back.</summary>
    public string? Reason { get; set; }

    /// <summary>The files sent, for a submission.</summary>
    public Guid[] AttachmentIds { get; set; } = [];
}
