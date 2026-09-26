import 'dart:convert';

import '../../../core/router/app_routes.dart';
import '../data/models/app_notification.dart';

/// Turns the JSON payload the API attaches to a notification into an in-app
/// route, or `null` when the notification has nowhere to go.
///
/// Directory routes are withheld from roles the API would answer 403 to, so a
/// tap never leads to a permission error.
String? deepLinkFor(
  AppNotification notification, {
  required bool canViewDirectory,
}) {
  final raw = notification.dataJson;

  if (raw == null || raw.isEmpty) {
    return null;
  }

  final Map<String, dynamic> data;

  try {
    final decoded = jsonDecode(raw);

    if (decoded is! Map<String, dynamic>) {
      return null;
    }

    data = decoded;
  } on FormatException {
    return null;
  }

  return deepLinkForData(
    notification.type,
    data,
    canViewDirectory: canViewDirectory,
  );
}

/// The same routing rules as [deepLinkFor], for a push notification's own
/// data map — an FCM `RemoteMessage.data` carries the identical keys the
/// stored notification's `dataJson` does, plus `notificationType`
/// (`ProcessOutboxCommand.SendPushAsync` adds it), so a push tap resolves a
/// destination the same way a tap in the in-app inbox does, without waiting
/// for the inbox to be re-fetched first.
String? deepLinkForData(
  String? type,
  Map<String, dynamic> data, {
  required bool canViewDirectory,
}) {
  // What concerns the person's own work needs no directory access, so a
  // worker's tap always lands somewhere useful.
  final own = switch (type) {
    'AccommodationAssigned' => AppRoutes.myHousing,
    'TimeEntryRejected' || 'ShiftAutoClosed' => AppRoutes.timeEntries,
    'AbsenceDecided' || 'AbsenceEditProposed' => AppRoutes.absences,
    'ArticleOrderRequested' || 'ArticleOrderStatusChanged' => AppRoutes.articleOrders,
    'RefundRequested' || 'RefundDecided' => AppRoutes.refunds,
    'WeeklyReportDue' => AppRoutes.weeklyReports,
    'BulletinPosted' => AppRoutes.bulletin,
    'TaskAssigned' || 'DefectAssigned' || 'WorkItemDue' => AppRoutes.workItems,
    _ => null,
  };

  if (own != null) {
    return own;
  }

  if (!canViewDirectory) {
    return null;
  }

  return switch (type) {
    'ProjectAssigned' when data['projectId'] is String =>
      AppRoutes.projectDetail(data['projectId'] as String),
    'EmployeeAssigned' when data['employeeId'] is String =>
      AppRoutes.employeeDetail(data['employeeId'] as String),
    'VehicleAssigned' when data['vehicleId'] is String =>
      AppRoutes.vehicleDetail(data['vehicleId'] as String),
    'ToolAssigned' when data['toolId'] is String =>
      AppRoutes.toolDetail(data['toolId'] as String),
    'MaterialLowStock' when data['materialId'] is String =>
      AppRoutes.materialDetail(data['materialId'] as String),
    'VehicleExpenseRejected' || 'VehicleExpenseSubmitted' => AppRoutes.vehicleExpenses,
    _ => null,
  };
}
