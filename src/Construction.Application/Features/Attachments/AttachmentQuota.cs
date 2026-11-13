using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments;

/// <summary>
/// How much one record may hold, and how much one account may send in a day.
/// </summary>
/// <remarks>
/// The limits are generous for honest use (a site folder with a few hundred photographs, an office dropping a month of
/// scans) and exist so that one account, or one stolen session, cannot fill the disk. The rate limit of the upload
/// endpoint slows a burst down; these limits cap the total.
/// <para>
/// shortcut: fixed constants, and the day is the last 24 hours rather than a calendar day. Move them to configuration if
/// a customer ever needs different numbers.
/// </para>
/// </remarks>
public static class AttachmentQuota
{
    /// <summary>Most files one record (an employee, a project, a vehicle ...) may hold.</summary>
    public const int MaxFilesPerRecord = 200;

    /// <summary>Most files one account may add in 24 hours.</summary>
    public const int MaxFilesPerAccountPerDay = 300;

    /// <summary>Most bytes one account may add in 24 hours.</summary>
    public const long MaxBytesPerAccountPerDay = 1024L * 1024 * 1024;

    /// <summary>
    /// Refuses the upload when it would pass a limit. Counts what is already stored, so it is checked before the file
    /// is scanned or saved.
    /// </summary>
    public static async Task EnsureRoomAsync(
        IApplicationDbContext context,
        AttachmentOwnerType ownerType,
        Guid ownerId,
        Guid? accountUserId,
        long incomingBytes,
        int incomingFiles,
        CancellationToken cancellationToken)
    {
        // The storage key starts with the record, which is the one place that names every owner type the same way.
        var prefix = $"{AttachmentOwner.PathSegment(ownerType)}/{ownerId:N}/";

        var onRecord = await context.Attachments
            .AsNoTracking()
            .CountAsync(a => a.StorageKey.StartsWith(prefix), cancellationToken);

        if (onRecord + incomingFiles > MaxFilesPerRecord)
        {
            throw new ConflictException(
                $"This record already holds {onRecord} files; at most {MaxFilesPerRecord} are kept on one record. Remove some first.");
        }

        if (accountUserId is not { } userId)
        {
            return;
        }

        var since = DateTime.UtcNow.AddHours(-24);

        var today = await context.Attachments
            .AsNoTracking()
            .Where(a => a.UploadedByUserId == userId && a.CreatedAt >= since)
            .GroupBy(_ => 1)
            .Select(g => new { Files = g.Count(), Bytes = g.Sum(a => a.SizeBytes) })
            .FirstOrDefaultAsync(cancellationToken);

        if ((today?.Files ?? 0) + incomingFiles > MaxFilesPerAccountPerDay
            || (today?.Bytes ?? 0) + incomingBytes > MaxBytesPerAccountPerDay)
        {
            throw new ConflictException("The daily limit for uploads has been reached. Try again tomorrow.");
        }
    }
}
