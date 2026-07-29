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

  if (raw == null || raw.isEmpty || !canViewDirectory) {
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

  return switch (notification.type) {
    'ProjectAssigned' when data['projectId'] is String =>
      AppRoutes.projectDetail(data['projectId'] as String),
    'EmployeeAssigned' when data['employeeId'] is String =>
      AppRoutes.employeeDetail(data['employeeId'] as String),
    _ => null,
  };
}
