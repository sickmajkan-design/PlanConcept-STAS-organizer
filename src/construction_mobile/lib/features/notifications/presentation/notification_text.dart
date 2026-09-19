import 'dart:convert';

import '../../../l10n/app_localizations.dart';
import '../data/models/app_notification.dart';

/// A notification's title and body, ready to show.
class LocalizedNotificationText {
  const LocalizedNotificationText({required this.title, required this.body});

  final String title;
  final String body;
}

/// Renders a notification in the reader's own language, for the system-
/// generated types that carry enough structured data in
/// `notification.dataJson` to reconstruct a full sentence from — a general
/// announcement, a direct message and a bulletin post are free text someone
/// actually typed, in whatever language they typed it, and are shown exactly
/// as stored; every other type is a fixed sentence shape the server fills in
/// with facts (a name, a date, an address), which is exactly what this
/// reconstructs in the reader's own language instead of the server's fixed
/// English.
///
/// The server still writes a plain-English `title`/`body` on every
/// notification — that is what desktop shows, unchanged, and what this falls
/// back to for a type this build does not know how to reconstruct, or when
/// the expected data is missing (an older notification sent before a
/// backend enrichment shipped, say). Extending this to a new type only ever
/// means adding a case here plus, on the backend, making sure that type's
/// `data` dictionary carries the values the case reads — never a change to
/// what is already stored.
LocalizedNotificationText resolveNotificationText(
  AppLocalizations l10n,
  AppNotification notification,
) {
  final data = _decodeData(notification.dataJson);
  final fallback = LocalizedNotificationText(
    title: notification.title,
    body: notification.body,
  );

  String? str(String key) => data?[key] as String?;

  switch (notification.type) {
    case 'ProjectAssigned':
      {
        final projectName = str('projectName');
        if (projectName == null) return fallback;

        final address = str('projectAddress');
        final shiftStartTime = str('projectShiftStartTime');

        final String body;
        if (address == null) {
          body = l10n.notificationProjectAssignedBody(projectName);
        } else if (shiftStartTime == null) {
          body = l10n.notificationProjectAssignedBodyWithAddress(projectName, address);
        } else {
          body = l10n.notificationProjectAssignedBodyFull(projectName, address, shiftStartTime);
        }

        return LocalizedNotificationText(title: l10n.notificationProjectAssignedTitle, body: body);
      }

    case 'EmployeeAssigned':
      {
        final employeeName = str('employeeName');
        final projectName = str('projectName');
        if (employeeName == null || projectName == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationEmployeeAssignedTitle,
          body: l10n.notificationEmployeeAssignedBody(employeeName, projectName),
        );
      }

    case 'VehicleAssigned':
      {
        final brand = str('vehicleBrand');
        final model = str('vehicleModel');
        final registration = str('vehicleRegistration');
        if (brand == null || model == null || registration == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationVehicleAssignedTitle,
          body: l10n.notificationVehicleAssignedBody(brand, model, registration),
        );
      }

    case 'ToolAssigned':
      {
        final toolName = str('toolName');
        if (toolName == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationToolAssignedTitle,
          body: l10n.notificationToolAssignedBody(toolName),
        );
      }

    case 'DocumentExpiring':
      {
        final fileName = str('fileName');
        final expiresAt = str('expiresAt');
        if (fileName == null || expiresAt == null) return fallback;

        final ownerName = str('ownerName');
        final expired = str('expired') == 'true';
        final date = _isoDate(expiresAt);

        final body = ownerName == null
            ? l10n.notificationDocumentExpiringBody(fileName, date)
            : l10n.notificationDocumentExpiringBodyWithOwner(fileName, ownerName, date);

        return LocalizedNotificationText(
          title: expired
              ? l10n.notificationDocumentExpiredTitle
              : l10n.notificationDocumentExpiringTitle,
          body: body,
        );
      }

    case 'TaskAssigned':
    case 'DefectAssigned':
      {
        final title = str('title');
        if (title == null) return fallback;

        final dueDate = str('dueDate');
        final isDefect = notification.type == 'DefectAssigned';

        final body = dueDate == null
            ? title
            : l10n.notificationWorkItemAssignedBodyWithDueDate(title, _isoDate(dueDate));

        return LocalizedNotificationText(
          title: isDefect
              ? l10n.notificationDefectAssignedTitle
              : l10n.notificationTaskAssignedTitle,
          body: body,
        );
      }

    case 'WorkItemDue':
      {
        final title = str('title');
        final dueDate = str('dueDate');
        if (title == null || dueDate == null) return fallback;

        final overdue = str('overdue') == 'true';

        return LocalizedNotificationText(
          title: overdue ? l10n.notificationWorkItemOverdueTitle : l10n.notificationWorkItemDueSoonTitle,
          body: l10n.notificationWorkItemDueBody(title, _isoDate(dueDate)),
        );
      }

    case 'ShiftAutoClosed':
      {
        final shiftDate = str('shiftDate');
        if (shiftDate == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationShiftAutoClosedTitle,
          body: l10n.notificationShiftAutoClosedBody(_isoDate(shiftDate)),
        );
      }

    case 'AbsenceEditProposed':
      {
        final startDate = str('startDate');
        final endDate = str('endDate');
        final approved = str('approved');

        if (approved == 'true' && startDate != null && endDate != null) {
          return LocalizedNotificationText(
            title: l10n.notificationAbsenceEditConfirmedTitle,
            body: l10n.notificationAbsenceEditConfirmedBody(_isoDate(startDate), _isoDate(endDate)),
          );
        }

        if (approved == 'false') {
          return LocalizedNotificationText(
            title: l10n.notificationAbsenceEditDeclinedTitle,
            body: l10n.notificationAbsenceEditDeclinedBody,
          );
        }

        if (startDate != null && endDate != null) {
          return LocalizedNotificationText(
            title: l10n.notificationAbsenceEditProposedTitle,
            body: l10n.notificationAbsenceEditProposedBody(_isoDate(startDate), _isoDate(endDate)),
          );
        }

        return fallback;
      }

    case 'WeeklyReportDue':
      {
        final projectName = str('projectName');
        final isoYear = str('isoYear');
        final isoWeek = str('isoWeek');
        if (projectName == null || isoYear == null || isoWeek == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationWeeklyReportDueTitle,
          body: l10n.notificationWeeklyReportDueBody(projectName, isoWeek, isoYear),
        );
      }

    case 'EmployeeClockedIn':
      {
        final employeeName = str('employeeName');
        final projectName = str('projectName');
        if (employeeName == null || projectName == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationEmployeeClockedInTitle,
          body: l10n.notificationEmployeeClockedInBody(employeeName, projectName),
        );
      }

    case 'EmployeeClockedOut':
      {
        final employeeName = str('employeeName');
        final projectName = str('projectName');
        final workedHours = str('workedHours');
        final workedMinutes = str('workedMinutes');
        if (employeeName == null || projectName == null || workedHours == null || workedMinutes == null) {
          return fallback;
        }

        return LocalizedNotificationText(
          title: l10n.notificationEmployeeClockedOutTitle,
          body: l10n.notificationEmployeeClockedOutBody(
            employeeName,
            projectName,
            workedHours,
            workedMinutes,
          ),
        );
      }

    case 'UnassignedProjectClockIn':
      {
        final employeeName = str('employeeName');
        final projectName = str('projectName');
        if (employeeName == null || projectName == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationUnassignedClockInTitle,
          body: l10n.notificationUnassignedClockInBody(employeeName, projectName),
        );
      }

    case 'ClockInLocationMismatch':
      {
        final employeeName = str('employeeName');
        final projectName = str('projectName');
        if (employeeName == null || projectName == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationClockInLocationMismatchTitle,
          body: l10n.notificationClockInLocationMismatchBody(employeeName, projectName),
        );
      }

    case 'DefectReported':
      {
        final reporterName = str('reporterName');
        final title = str('title');
        if (reporterName == null || title == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationDefectReportedTitle,
          body: l10n.notificationDefectReportedBody(reporterName, title),
        );
      }

    case 'MaterialLowStock':
      {
        final materialName = str('materialName');
        final quantity = str('quantity');
        final unit = str('unit');
        final minimum = str('minimum');
        if (materialName == null || quantity == null || unit == null || minimum == null) {
          return fallback;
        }

        return LocalizedNotificationText(
          title: l10n.notificationMaterialLowStockTitle,
          body: l10n.notificationMaterialLowStockBody(materialName, quantity, unit, minimum),
        );
      }

    case 'TimeEntryRejected':
      {
        final date = str('date');
        final note = str('note');
        if (date == null || note == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationTimeEntryRejectedTitle,
          body: l10n.notificationTimeEntryRejectedBody(_isoDate(date), note),
        );
      }

    case 'AbsenceDecided':
      {
        final decision = str('decision');
        final startDate = str('startDate');
        final endDate = str('endDate');
        if (decision == null || startDate == null || endDate == null) return fallback;

        final start = _isoDate(startDate);
        final end = _isoDate(endDate);
        if (decision == 'Approved') {
          return LocalizedNotificationText(
            title: l10n.notificationAbsenceApprovedTitle,
            body: l10n.notificationAbsenceApprovedBody(start, end),
          );
        }

        final note = str('note');
        return LocalizedNotificationText(
          title: l10n.notificationAbsenceRefusedTitle,
          body: (note == null || note.isEmpty)
              ? l10n.notificationAbsenceRefusedBodyNoReason(start, end)
              : l10n.notificationAbsenceRefusedBody(start, end, note),
        );
      }

    case 'VehicleExpenseSubmitted':
      {
        final count = int.tryParse(str('count') ?? '');
        if (count != null && count > 1) {
          return LocalizedNotificationText(
            title: l10n.notificationVehicleExpenseSubmittedBulkTitle,
            body: l10n.notificationVehicleExpenseSubmittedBulkBody(count),
          );
        }

        final vehicleName = str('vehicleName');
        final occurredOn = str('occurredOn');
        if (vehicleName == null || occurredOn == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationVehicleExpenseSubmittedTitle,
          body: l10n.notificationVehicleExpenseSubmittedBody(
            vehicleName,
            _isoDate(occurredOn),
          ),
        );
      }

    case 'VehicleExpenseRejected':
      {
        final vehicleName = str('vehicleName');
        final occurredOn = str('occurredOn');
        final note = str('note');
        if (vehicleName == null || occurredOn == null || note == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationVehicleExpenseRejectedTitle,
          body: l10n.notificationVehicleExpenseRejectedBody(
            vehicleName,
            _isoDate(occurredOn),
            note,
          ),
        );
      }

    case 'AbsenceRequested':
      {
        final employeeName = str('employeeName');
        final startDate = str('startDate');
        final endDate = str('endDate');
        if (employeeName == null || startDate == null || endDate == null) return fallback;

        return LocalizedNotificationText(
          title: l10n.notificationAbsenceRequestedTitle,
          body: l10n.notificationAbsenceRequestedBody(employeeName, _isoDate(startDate), _isoDate(endDate)),
        );
      }

    case 'DocumentRetentionEnded':
      {
        final fileName = str('fileName');
        final retainUntil = str('retainUntil');
        if (fileName == null || retainUntil == null) return fallback;

        final ownerName = str('ownerName');
        final date = _isoDate(retainUntil);

        return LocalizedNotificationText(
          title: l10n.notificationDocumentRetentionEndedTitle,
          body: ownerName == null
              ? l10n.notificationDocumentRetentionEndedBody(fileName, date)
              : l10n.notificationDocumentRetentionEndedBodyWithOwner(fileName, ownerName, date),
        );
      }
  }

  return fallback;
}

Map<String, dynamic>? _decodeData(String? raw) {
  if (raw == null || raw.isEmpty) {
    return null;
  }

  try {
    final decoded = jsonDecode(raw);
    return decoded is Map<String, dynamic> ? decoded : null;
  } on FormatException {
    return null;
  }
}

/// `yyyy-MM-dd` (as the backend sends dates in `data`) to the day-first
/// `dd.MM.yyyy.` convention already used everywhere else in this app
/// (`formatDate` in `core/utils/formatting.dart`) — done by splitting the
/// string rather than parsing it as a `DateTime`, since a calendar date has
/// no timezone to get wrong by doing that.
String _isoDate(String iso) {
  final parts = iso.split('-');
  if (parts.length != 3) return iso;
  return '${parts[2]}.${parts[1]}.${parts[0]}.';
}
