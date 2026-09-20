import type { Notification, User } from '../../api/types';
import { canAdministerAccounts, canViewDirectory } from '../../auth/authHelpers';
import { paths } from '../../routes/paths';

function parseData(dataJson: string | null): Record<string, string> {
  if (!dataJson) return {};

  try {
    const parsed: unknown = JSON.parse(dataJson);
    return parsed && typeof parsed === 'object' ? (parsed as Record<string, string>) : {};
  } catch {
    return {};
  }
}

/** The local day a notification landed on, for types whose payload has no date of its own. */
function notificationDate(notification: Notification): string {
  return notification.createdAt.slice(0, 10);
}

/**
 * Where a notification's own "open it" click should land — the page for the
 * thing that raised it, not just the inbox it sits in. Mirrors the mobile
 * app's `notification_deep_link.dart` (same `dataJson` payload, same
 * per-type routing), extended to every type the API sends and to this
 * panel's own route catalogue.
 *
 * Returns `null` when there is nowhere to go (a free-typed message has no
 * source entity) or the viewer's role cannot reach that route — a click must
 * never send someone into a page the API would 403 them on.
 */
export function resolveNotificationTarget(
  notification: Notification,
  user: User | null | undefined,
): string | null {
  const data = parseData(notification.dataJson);
  const hasDirectory = canViewDirectory(user);
  const hasAccountAdmin = canAdministerAccounts(user);

  switch (notification.type) {
    case 'ProjectAssigned':
    case 'WeeklyReportDue':
      return hasDirectory && data.projectId ? paths.projectDetail(data.projectId) : null;

    case 'EmployeeAssigned':
      return hasDirectory && data.employeeId ? paths.employeeDetail(data.employeeId) : null;

    case 'VehicleAssigned':
      return hasDirectory && data.vehicleId ? paths.vehicleDetail(data.vehicleId) : null;

    case 'ToolAssigned':
      return hasDirectory && data.toolId ? paths.toolDetail(data.toolId) : null;

    case 'TaskAssigned':
    case 'DefectAssigned':
    case 'DefectReported':
    case 'WorkItemDue':
      return hasDirectory && data.workItemId ? paths.workItemEdit(data.workItemId) : null;

    case 'ShiftAutoClosed':
      return hasDirectory && data.timeEntryId ? paths.timeEntryEdit(data.timeEntryId) : null;

    // No time-entry id in these payloads (the entry is created by the clock-in
    // itself), so the deep link opens the day/project/employee it happened on
    // instead — TimeEntriesListPage expands that project's column and flashes
    // the matching card.
    case 'EmployeeClockedIn':
    case 'EmployeeClockedOut':
    case 'ClockInLocationMismatch':
    case 'UnassignedProjectClockIn': {
      if (!hasDirectory || !data.employeeId) return null;
      const params = new URLSearchParams({
        employeeId: data.employeeId,
        date: notificationDate(notification),
      });
      if (data.projectId) params.set('projectId', data.projectId);
      return `${paths.timeEntries}?${params.toString()}`;
    }

    // The costs page has no per-row highlight yet, so this lands on the list,
    // where the rejected cost carries the "Rejected" status.
    case 'VehicleExpenseRejected':
    case 'VehicleExpenseSubmitted':
      return hasDirectory ? paths.vehicleExpenses : null;

    case 'AccommodationContractExpiring':
      return hasDirectory && data.accommodationId ? paths.accommodationDetail(data.accommodationId) : null;

    case 'MaterialLowStock':
      return hasDirectory && data.materialId ? paths.materialDetail(data.materialId) : null;

    case 'TimeEntryRejected':
      return hasDirectory ? paths.timeEntries : null;

    case 'AbsenceDecided':
      if (!hasDirectory) return null;
      return data.absenceId ? `${paths.absences}?highlight=${data.absenceId}` : paths.absences;

    case 'AbsenceRequested':
    case 'AbsenceEditProposed':
      if (!hasDirectory) return null;
      return data.absenceId ? `${paths.absences}?highlight=${data.absenceId}` : paths.absences;

    case 'BulletinPosted':
      return data.bulletinPostId
        ? `${paths.bulletin}?highlight=${data.bulletinPostId}`
        : paths.bulletin;

    case 'DocumentExpiring':
    case 'DocumentRetentionEnded':
      if (!hasAccountAdmin) return null;
      return data.attachmentId
        ? `${paths.expiringDocuments}?highlight=${data.attachmentId}`
        : paths.expiringDocuments;

    // Free-typed text with no source entity to open.
    case 'GeneralAnnouncement':
    case 'DirectMessage':
    default:
      return null;
  }
}
