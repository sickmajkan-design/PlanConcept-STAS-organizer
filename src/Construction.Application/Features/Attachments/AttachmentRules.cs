using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Attachments;

/// <summary>
/// What may be uploaded, and who may see it once it is.
/// </summary>
public static class AttachmentRules
{
    /// <summary>
    /// Largest file accepted.
    /// </summary>
    /// <remarks>
    /// Sized for a scanned multi-page document and a phone photograph, which
    /// is everything this product stores. Anything larger is a video or a
    /// mistake, and both are better refused at the door than discovered when
    /// the disk fills.
    /// </remarks>
    public const long MaxSizeBytes = 20 * 1024 * 1024;

    /// <summary>
    /// Content types accepted, as an allow-list.
    /// </summary>
    /// <remarks>
    /// An allow-list rather than a block-list of dangerous extensions: the
    /// block-list is always one entry short, and the set of things a site
    /// office genuinely needs to attach is small and stable. The type is also
    /// re-derived from the file name rather than trusted from the request —
    /// see <see cref="ResolveContentType"/>.
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, string> AllowedTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".heic"] = "image/heic",
            [".doc"] = "application/msword",
            [".docx"] =
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] =
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".txt"] = "text/plain"
        };

    /// <summary>
    /// The content type for a file name, or null when the extension is not
    /// one this product accepts.
    /// </summary>
    /// <remarks>
    /// Derived from the extension rather than taken from the upload's own
    /// Content-Type header, which is set by the client and therefore says
    /// whatever the client wants. A browser told a file is an image will try
    /// to render it, so the value that decides that must come from us.
    /// </remarks>
    public static string? ResolveContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return AllowedTypesByExtension.TryGetValue(extension, out var contentType)
            ? contentType
            : null;
    }

    /// <summary>
    /// Strips a name down to something safe to send back in a header.
    /// </summary>
    /// <remarks>
    /// The name is never used as a path — the storage key is generated — but
    /// it does travel in `Content-Disposition`, where a newline would let an
    /// uploader inject a header of their own.
    /// </remarks>
    public static string SanitiseFileName(string fileName)
    {
        // Both separators, explicitly, rather than Path.GetFileName: that
        // honours only the separator of the platform the API happens to run
        // on, so a Windows browser sending a full path would leave a Linux
        // server with the whole thing as the "name".
        var lastSeparator = fileName.LastIndexOfAny(['/', '\\']);

        var name = lastSeparator < 0
            ? fileName
            : fileName[(lastSeparator + 1)..];

        var cleaned = new string(name
            .Trim()
            .Where(c => !char.IsControl(c) && c != '"')
            .ToArray());

        return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
    }

    /// <summary>Where the bytes are filed. Generated, never client-supplied.</summary>
    public static string BuildStorageKey(
        AttachmentOwnerType ownerType,
        Guid ownerId,
        Guid attachmentId,
        string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return $"{AttachmentOwner.PathSegment(ownerType)}/{ownerId:N}/{attachmentId:N}{extension}";
    }

    /// <summary>
    /// True when <paramref name="role"/> may read files on this kind of owner.
    /// </summary>
    /// <remarks>
    /// Employee files are the sensitive ones — contracts and occupational
    /// medical checks — so they stop at Admin, one rung above the directory
    /// screens. Everyone else's own record is handled separately by
    /// <see cref="CanReadOwnEmployeeFiles"/>: a worker may read their own
    /// medical certificate, which is theirs, without being able to read
    /// anybody else's.
    /// </remarks>
    public static bool CanRead(UserRole? role, AttachmentOwnerType ownerType) =>
        ownerType == AttachmentOwnerType.Employee
            ? role is UserRole.SuperAdmin or UserRole.Admin
            : role is UserRole.SuperAdmin or UserRole.Admin
                or UserRole.ProjectManager or UserRole.Foreman;

    /// <summary>
    /// An employee may always read what is filed against them.
    /// </summary>
    public static bool CanReadOwnEmployeeFiles(
        Guid? callerEmployeeId,
        Attachment attachment) =>
        callerEmployeeId is not null
        && attachment.EmployeeId == callerEmployeeId;

    /// <summary>
    /// True when <paramref name="role"/> may attach this category here.
    /// </summary>
    /// <remarks>
    /// Uploads are Foreman and above, with one deliberate exception: a Worker
    /// may add a photograph to a project. Site photographs are the one thing
    /// the person holding the phone is best placed to capture, and adding a
    /// file cannot overwrite or remove anything.
    ///
    /// The exception is not scoped to projects the worker is assigned to,
    /// because assignment-level authorization does not exist yet (H11 in the
    /// audit). The consequence is bounded — a worker could file a photo
    /// against the wrong site — and is recorded rather than hidden.
    /// </remarks>
    public static bool CanUpload(
        UserRole? role,
        AttachmentOwnerType ownerType,
        AttachmentCategory category)
    {
        if (role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman)
        {
            return true;
        }

        return role is UserRole.Worker
            && ownerType == AttachmentOwnerType.Project
            && category == AttachmentCategory.Photo;
    }

    /// <summary>Deleting a file is Admin and above, whatever it hangs off.</summary>
    public static bool CanDelete(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin;
}
