using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Attachments;

/// <summary>
/// Leaves a mark in the audit trail whenever somebody other than the person themselves opens a document
/// that is personal data of a special kind — a medical check, an identity document, a visa.
/// </summary>
/// <remarks>
/// The trail so far recorded changes only; nobody could say who had looked at a worker's medical
/// certificate. Reading is the event data-protection law cares about for health data, so it is recorded
/// with who, when and from where. It is written with the same context save that serves the file, so a
/// read that is recorded is a read that happened, and the other way round.
///
/// A person opening their own document is not recorded: it is theirs, and it would only bury the entries
/// that matter.
/// </remarks>
public static class SensitiveReadAudit
{
    /// <summary>The categories whose reading is recorded.</summary>
    public static bool IsSensitive(AttachmentCategory category) =>
        category is AttachmentCategory.MedicalCheck
            or AttachmentCategory.Identity
            or AttachmentCategory.Visa;

    /// <summary>
    /// Adds an entry to the context for this reading, when it is one that is recorded. The caller saves.
    /// </summary>
    public static void Record(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        Guid attachmentId,
        AttachmentCategory category,
        Guid? attachmentEmployeeId,
        string how)
    {
        if (!IsSensitive(category))
        {
            return;
        }

        // Their own: not recorded.
        if (attachmentEmployeeId is not null && attachmentEmployeeId == currentUser.EmployeeId)
        {
            return;
        }

        var owner = attachmentEmployeeId?.ToString() ?? string.Empty;

        context.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = clock.UtcNow,
            Action = AuditAction.Viewed,
            EntityName = "Attachment",
            EntityId = attachmentId,
            UserId = currentUser.UserId,
            UserEmail = currentUser.Email,
            UserRole = currentUser.Role,
            IpAddress = currentUser.IpAddress,
            ChangesJson =
                "{\"Category\":{\"from\":null,\"to\":\"" + category + "\"},"
                + "\"Employee\":{\"from\":null,\"to\":\"" + owner + "\"},"
                + "\"How\":{\"from\":null,\"to\":\"" + how + "\"}}"
        });
    }
}
