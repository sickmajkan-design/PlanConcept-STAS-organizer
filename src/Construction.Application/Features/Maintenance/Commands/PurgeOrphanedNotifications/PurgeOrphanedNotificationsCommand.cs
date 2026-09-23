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
public record PurgeOrphanedNotificationsCommand : IRequest<int>;

public class PurgeOrphanedNotificationsCommandHandler
    : IRequestHandler<PurgeOrphanedNotificationsCommand, int>
{
    /// <summary>
    /// The single id inside each type's <c>DataJson</c> that decides whether
    /// the notification still points at something real. A few types carry a
    /// second id alongside this one (e.g. <c>ClockIn</c>'s own
    /// notifications also record a project) — only the one the frontend's
    /// deep-link resolver actually navigates to is checked here, because
    /// that is the one whose absence is what "this notification is broken"
    /// means in practice.
    /// </summary>
    private static readonly IReadOnlyDictionary<NotificationType, string> ReferenceKeyByType =
        new Dictionary<NotificationType, string>
        {
            [NotificationType.ProjectAssigned] = "projectId",
            [NotificationType.WeeklyReportDue] = "projectId",
            [NotificationType.EmployeeAssigned] = "employeeId",
            [NotificationType.EmployeeClockedIn] = "employeeId",
            [NotificationType.EmployeeClockedOut] = "employeeId",
            [NotificationType.UnassignedProjectClockIn] = "employeeId",
            [NotificationType.ClockInLocationMismatch] = "employeeId",
            [NotificationType.VehicleAssigned] = "vehicleId",
            [NotificationType.ToolAssigned] = "toolId",
            [NotificationType.DocumentExpiring] = "attachmentId",
            [NotificationType.DocumentRetentionEnded] = "attachmentId",
            [NotificationType.TaskAssigned] = "workItemId",
            [NotificationType.DefectAssigned] = "workItemId",
            [NotificationType.DefectReported] = "workItemId",
            [NotificationType.WorkItemDue] = "workItemId",
            [NotificationType.ShiftAutoClosed] = "timeEntryId",
            [NotificationType.BulletinPosted] = "bulletinPostId",
            [NotificationType.AbsenceEditProposed] = "absenceId",
            [NotificationType.AbsenceRequested] = "absenceId",
            [NotificationType.AbsenceDecided] = "absenceId",
            [NotificationType.MaterialLowStock] = "materialId",
            [NotificationType.AccommodationContractExpiring] = "accommodationId",
            [NotificationType.AccommodationAssigned] = "accommodationId",

            // Deliberately absent: GeneralAnnouncement and DirectMessage carry no
            // entity reference at all (free text), and VehicleExpenseSubmitted/
            // Rejected and TimeEntryRejected route to a list page rather than one
            // record, so there is no single id whose disappearance makes them stale.
        };

    private readonly IApplicationDbContext _context;

    public PurgeOrphanedNotificationsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        PurgeOrphanedNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        var checkableTypes = ReferenceKeyByType.Keys.ToList();

        var candidates = await _context.Notifications
            .Where(n => n.DataJson != null && checkableTypes.Contains(n.Type))
            .Select(n => new { n.Id, n.Type, n.DataJson })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        // One (notification id, reference key, referenced id) tuple per
        // candidate, so each notification is judged against the same key it
        // was grouped and checked by below.
        var references = new List<(Guid NotificationId, string Key, Guid RefId)>();
        var idsByKey = new Dictionary<string, HashSet<Guid>>();

        foreach (var candidate in candidates)
        {
            var key = ReferenceKeyByType[candidate.Type];

            if (!TryExtractId(candidate.DataJson!, key, out var refId))
            {
                // Missing or unparseable — nothing to check this one against,
                // so it is left alone rather than guessed at.
                continue;
            }

            references.Add((candidate.Id, key, refId));

            if (!idsByKey.TryGetValue(key, out var set))
            {
                idsByKey[key] = set = [];
            }

            set.Add(refId);
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

    private static bool TryExtractId(string dataJson, string key, out Guid id)
    {
        id = Guid.Empty;

        try
        {
            using var document = JsonDocument.Parse(dataJson);

            return document.RootElement.TryGetProperty(key, out var property)
                && Guid.TryParse(property.GetString(), out id);
        }
        catch (JsonException)
        {
            return false;
        }
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
