using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Maintenance.Commands.PurgeOrphanedNotifications;

/// <summary>
/// Removes notifications whose referenced record no longer exists.
/// </summary>
/// <remarks>
/// <para>
/// A notification carries no foreign key to the thing it is about — an
/// absence, a document, a work item — only a plain id string inside
/// <see cref="Construction.Domain.Entities.Notification.DataJson"/>. That
/// was a deliberate choice (one polymorphic column instead of a dozen
/// nullable FK columns, one per notification type), but it means the
/// database itself has no way to notice when the row that id points to is
/// deleted, and no <c>ON DELETE CASCADE</c> can exist for a reference that
/// isn't a real foreign key. Deleting a row directly in the database — or,
/// really, any deletion path that skips a dedicated cleanup step — leaves
/// the notification behind, pointing at nothing, looking exactly as active
/// as it did before.
/// </para>
/// <para>
/// This sweep is the fix: for every notification type that carries a
/// checkable id, re-derive whether that id still exists as a live row and
/// delete the notification if it does not. It reads through each entity's
/// own soft-delete filter (the default for its DbSet), so a soft-deleted
/// record counts as gone here too — a notification about an archived
/// employee is exactly as stale as one about a physically deleted one.
/// </para>
/// </remarks>
public record PurgeOrphanedNotificationsCommand : IRequest<int>
{
    /// <summary>
    /// Runs the sweep for a caller that only wants a fresh view: best effort,
    /// so a failure here never breaks the request that asked for it.
    /// </summary>
    public static async Task TryRunAsync(ISender sender, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new PurgeOrphanedNotificationsCommand(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The inbox is still correct as of the last sweep.
        }
    }
}

public class PurgeOrphanedNotificationsCommandHandler
    : IRequestHandler<PurgeOrphanedNotificationsCommand, int>
{
    /// <summary>
    /// Every id a notification's <c>DataJson</c> can point at. A notification is
    /// stale as soon as <em>any</em> of them refers to a record that is gone —
    /// "new project assigned" carries both a project and the employee it was
    /// sent about, and deleting either one makes it wrong. Types with no
    /// reference at all (announcements, direct messages) have none of these keys
    /// and are never touched.
    /// </summary>
    private static readonly string[] ReferenceKeys =
    [
        "projectId", "employeeId", "vehicleId", "toolId", "attachmentId", "workItemId",
        "timeEntryId", "bulletinPostId", "absenceId", "materialId", "accommodationId",
    ];

    private readonly IApplicationDbContext _context;

    public PurgeOrphanedNotificationsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        PurgeOrphanedNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        var candidates = await _context.Notifications
            .Where(n => n.DataJson != null)
            .Select(n => new { n.Id, n.DataJson })
            .ToListAsync(cancellationToken);

        // (notification id, reference key, referenced id) — one per id found.
        var references = new List<(Guid NotificationId, string Key, Guid RefId)>();
        var idsByKey = new Dictionary<string, HashSet<Guid>>();

        foreach (var candidate in candidates)
        {
            foreach (var (key, refId) in ExtractReferences(candidate.DataJson!))
            {
                references.Add((candidate.Id, key, refId));

                if (!idsByKey.TryGetValue(key, out var set))
                {
                    idsByKey[key] = set = [];
                }

                set.Add(refId);
            }
        }

        if (references.Count == 0)
        {
            return 0;
        }

        var existingIdsByKey = new Dictionary<string, HashSet<Guid>>();

        foreach (var (key, ids) in idsByKey)
        {
            existingIdsByKey[key] = await ExistingIdsAsync(key, ids, cancellationToken);
        }

        var orphanedIds = references
            .Where(r => !existingIdsByKey[r.Key].Contains(r.RefId))
            .Select(r => r.NotificationId)
            .ToList();

        if (orphanedIds.Count == 0)
        {
            return 0;
        }

        return await _context.Notifications
            .Where(n => orphanedIds.Contains(n.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static IEnumerable<(string Key, Guid Id)> ExtractReferences(string dataJson)
    {
        var found = new List<(string, Guid)>();

        try
        {
            using var document = JsonDocument.Parse(dataJson);

            foreach (var key in ReferenceKeys)
            {
                if (document.RootElement.TryGetProperty(key, out var property)
                    && Guid.TryParse(property.GetString(), out var id))
                {
                    found.Add((key, id));
                }
            }
        }
        catch (JsonException)
        {
            // Unparseable — nothing to check it against, so it is left alone.
        }

        return found;
    }

    /// <summary>
    /// Which of these ids still exist as a live row, reading through each
    /// entity's own soft-delete query filter — the same "does this still
    /// count as existing" rule every other feature already uses.
    /// </summary>
    private Task<HashSet<Guid>> ExistingIdsAsync(
        string key, HashSet<Guid> ids, CancellationToken cancellationToken)
    {
        IQueryable<Guid> query = key switch
        {
            "projectId" => _context.Projects.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "employeeId" => _context.Employees.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "vehicleId" => _context.Vehicles.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "toolId" => _context.Tools.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "attachmentId" => _context.Attachments.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "workItemId" => _context.WorkItems.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "timeEntryId" => _context.TimeEntries.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "bulletinPostId" => _context.BulletinPosts.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "absenceId" => _context.Absences.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "materialId" => _context.Materials.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            "accommodationId" => _context.Accommodations.Where(x => ids.Contains(x.Id)).Select(x => x.Id),
            _ => Enumerable.Empty<Guid>().AsQueryable(),
        };

        return query.ToHashSetAsync(cancellationToken);
    }
}
