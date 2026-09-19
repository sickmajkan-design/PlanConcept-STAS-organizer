import type { Notification } from '../../api/types';
import type { MessageKey } from '../../i18n/en';
import type { useT } from '../../i18n/useI18n';

type T = ReturnType<typeof useT>;

export interface NotificationText {
  title: string;
  body: string;
}

const key = (name: string) => `notificationText.${name}` as MessageKey;

/** `yyyy-MM-dd` to the day-first form used everywhere else. A calendar date has no timezone, so it is split, not parsed. */
function isoDate(iso: string): string {
  const parts = iso.split('-');
  return parts.length === 3 ? `${parts[2]}.${parts[1]}.${parts[0]}.` : iso;
}

function parse(raw: string | null | undefined): Record<string, string> {
  if (!raw) return {};

  try {
    const value: unknown = JSON.parse(raw);
    return value && typeof value === 'object' ? (value as Record<string, string>) : {};
  } catch {
    return {};
  }
}

/**
 * A notification's title and body in the reader's own language.
 *
 * The server writes one English sentence per notification. That is what the
 * inbox used to show whichever language the panel was in. This rebuilds the
 * sentence from the facts stored with it (names, dates), the same way the
 * mobile app does, and falls back to what the server wrote for a type it does
 * not know or when a fact is missing (an older notification). Free text an
 * operator typed (announcements, messages, bulletin posts) is shown as sent.
 */
export function resolveNotificationText(t: T, notification: Notification): NotificationText {
  const fallback = { title: notification.title, body: notification.body };
  const d = parse(notification.dataJson);
  const text = (name: string, values?: Record<string, string | number>) =>
    t(key(name), values);

  switch (notification.type) {
    case 'ProjectAssigned': {
      if (!d.projectName) return fallback;
      const values = {
        projectName: d.projectName,
        address: d.projectAddress ?? '',
        shiftStartTime: d.projectShiftStartTime ?? '',
      };
      const body = !d.projectAddress
        ? text('notificationProjectAssignedBody', values)
        : !d.projectShiftStartTime
          ? text('notificationProjectAssignedBodyWithAddress', values)
          : text('notificationProjectAssignedBodyFull', values);

      return { title: text('notificationProjectAssignedTitle'), body };
    }

    case 'EmployeeAssigned':
      if (!d.employeeName || !d.projectName) return fallback;
      return {
        title: text('notificationEmployeeAssignedTitle'),
        body: text('notificationEmployeeAssignedBody', {
          employeeName: d.employeeName,
          projectName: d.projectName,
        }),
      };

    case 'VehicleAssigned':
      if (!d.vehicleBrand || !d.vehicleModel || !d.vehicleRegistration) return fallback;
      return {
        title: text('notificationVehicleAssignedTitle'),
        body: text('notificationVehicleAssignedBody', {
          brand: d.vehicleBrand,
          model: d.vehicleModel,
          registration: d.vehicleRegistration,
        }),
      };

    case 'ToolAssigned':
      if (!d.toolName) return fallback;
      return {
        title: text('notificationToolAssignedTitle'),
        body: text('notificationToolAssignedBody', { toolName: d.toolName }),
      };

    case 'DocumentExpiring': {
      if (!d.fileName || !d.expiresAt) return fallback;
      const values = { fileName: d.fileName, ownerName: d.ownerName ?? '', expiresAt: isoDate(d.expiresAt) };

      return {
        title: text(
          d.expired === 'true' ? 'notificationDocumentExpiredTitle' : 'notificationDocumentExpiringTitle',
        ),
        body: text(
          d.ownerName ? 'notificationDocumentExpiringBodyWithOwner' : 'notificationDocumentExpiringBody',
          values,
        ),
      };
    }

    case 'TaskAssigned':
    case 'DefectAssigned':
      if (!d.title) return fallback;
      return {
        title: text(
          notification.type === 'DefectAssigned'
            ? 'notificationDefectAssignedTitle'
            : 'notificationTaskAssignedTitle',
        ),
        body: d.dueDate
          ? text('notificationWorkItemAssignedBodyWithDueDate', {
              title: d.title,
              dueDate: isoDate(d.dueDate),
            })
          : d.title,
      };

    case 'WorkItemDue':
      if (!d.title || !d.dueDate) return fallback;
      return {
        title: text(
          d.overdue === 'true' ? 'notificationWorkItemOverdueTitle' : 'notificationWorkItemDueSoonTitle',
        ),
        body: text('notificationWorkItemDueBody', { title: d.title, dueDate: isoDate(d.dueDate) }),
      };

    case 'ShiftAutoClosed':
      if (!d.shiftDate) return fallback;
      return {
        title: text('notificationShiftAutoClosedTitle'),
        body: text('notificationShiftAutoClosedBody', { shiftDate: isoDate(d.shiftDate) }),
      };

    case 'AbsenceEditProposed': {
      const dates =
        d.startDate && d.endDate
          ? { startDate: isoDate(d.startDate), endDate: isoDate(d.endDate) }
          : null;

      if (d.approved === 'true' && dates) {
        return {
          title: text('notificationAbsenceEditConfirmedTitle'),
          body: text('notificationAbsenceEditConfirmedBody', dates),
        };
      }

      if (d.approved === 'false') {
        return {
          title: text('notificationAbsenceEditDeclinedTitle'),
          body: text('notificationAbsenceEditDeclinedBody'),
        };
      }

      if (dates) {
        return {
          title: text('notificationAbsenceEditProposedTitle'),
          body: text('notificationAbsenceEditProposedBody', dates),
        };
      }

      return fallback;
    }

    case 'WeeklyReportDue':
      if (!d.projectName || !d.isoYear || !d.isoWeek) return fallback;
      return {
        title: text('notificationWeeklyReportDueTitle'),
        body: text('notificationWeeklyReportDueBody', {
          projectName: d.projectName,
          isoWeek: d.isoWeek,
          isoYear: d.isoYear,
        }),
      };

    case 'EmployeeClockedIn':
      if (!d.employeeName || !d.projectName) return fallback;
      return {
        title: text('notificationEmployeeClockedInTitle'),
        body: text('notificationEmployeeClockedInBody', {
          employeeName: d.employeeName,
          projectName: d.projectName,
        }),
      };

    case 'EmployeeClockedOut':
      if (!d.employeeName || !d.projectName || !d.workedHours || !d.workedMinutes) return fallback;
      return {
        title: text('notificationEmployeeClockedOutTitle'),
        body: text('notificationEmployeeClockedOutBody', {
          employeeName: d.employeeName,
          projectName: d.projectName,
          hours: d.workedHours,
          minutes: d.workedMinutes,
        }),
      };

    case 'UnassignedProjectClockIn':
      if (!d.employeeName || !d.projectName) return fallback;
      return {
        title: text('notificationUnassignedClockInTitle'),
        body: text('notificationUnassignedClockInBody', {
          employeeName: d.employeeName,
          projectName: d.projectName,
        }),
      };

    case 'ClockInLocationMismatch':
      if (!d.employeeName || !d.projectName) return fallback;
      return {
        title: text('notificationClockInLocationMismatchTitle'),
        body: text('notificationClockInLocationMismatchBody', {
          employeeName: d.employeeName,
          projectName: d.projectName,
        }),
      };

    case 'DefectReported':
      if (!d.reporterName || !d.title) return fallback;
      return {
        title: text('notificationDefectReportedTitle'),
        body: text('notificationDefectReportedBody', { reporterName: d.reporterName, title: d.title }),
      };

    case 'MaterialLowStock':
      if (!d.materialName || !d.quantity || !d.unit || !d.minimum) return fallback;
      return {
        title: text('notificationMaterialLowStockTitle'),
        body: text('notificationMaterialLowStockBody', {
          materialName: d.materialName,
          quantity: d.quantity,
          unit: d.unit,
          minimum: d.minimum,
        }),
      };

    case 'TimeEntryRejected':
      if (!d.date || d.note === undefined) return fallback;
      return {
        title: text('notificationTimeEntryRejectedTitle'),
        body: text('notificationTimeEntryRejectedBody', { date: isoDate(d.date), note: d.note }),
      };

    case 'AbsenceDecided': {
      if (!d.decision || !d.startDate || !d.endDate) return fallback;
      const dates = { startDate: isoDate(d.startDate), endDate: isoDate(d.endDate) };

      if (d.decision === 'Approved') {
        return {
          title: text('notificationAbsenceApprovedTitle'),
          body: text('notificationAbsenceApprovedBody', dates),
        };
      }

      return {
        title: text('notificationAbsenceRefusedTitle'),
        body: d.note
          ? text('notificationAbsenceRefusedBody', { ...dates, note: d.note })
          : text('notificationAbsenceRefusedBodyNoReason', dates),
      };
    }

    case 'VehicleExpenseSubmitted': {
      const count = Number(d.count);
      if (Number.isFinite(count) && count > 1) {
        return {
          title: text('notificationVehicleExpenseSubmittedBulkTitle'),
          body: text('notificationVehicleExpenseSubmittedBulkBody', { count }),
        };
      }

      if (!d.vehicleName || !d.occurredOn) return fallback;
      return {
        title: text('notificationVehicleExpenseSubmittedTitle'),
        body: text('notificationVehicleExpenseSubmittedBody', {
          vehicleName: d.vehicleName,
          date: isoDate(d.occurredOn),
        }),
      };
    }

    case 'VehicleExpenseRejected':
      if (!d.vehicleName || !d.occurredOn || !d.note) return fallback;
      return {
        title: text('notificationVehicleExpenseRejectedTitle'),
        body: text('notificationVehicleExpenseRejectedBody', {
          vehicleName: d.vehicleName,
          date: isoDate(d.occurredOn),
          note: d.note,
        }),
      };

    case 'AbsenceRequested':
      if (!d.employeeName || !d.startDate || !d.endDate) return fallback;
      return {
        title: text('notificationAbsenceRequestedTitle'),
        body: text('notificationAbsenceRequestedBody', {
          employeeName: d.employeeName,
          startDate: isoDate(d.startDate),
          endDate: isoDate(d.endDate),
        }),
      };

    case 'DocumentRetentionEnded':
      if (!d.fileName || !d.retainUntil) return fallback;
      return {
        title: text('notificationDocumentRetentionEndedTitle'),
        body: text(
          d.ownerName
            ? 'notificationDocumentRetentionEndedBodyWithOwner'
            : 'notificationDocumentRetentionEndedBody',
          { fileName: d.fileName, ownerName: d.ownerName ?? '', retainUntil: isoDate(d.retainUntil) },
        ),
      };

    default:
      return fallback;
  }
}
