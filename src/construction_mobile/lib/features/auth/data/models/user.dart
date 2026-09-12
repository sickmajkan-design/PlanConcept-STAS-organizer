import 'package:freezed_annotation/freezed_annotation.dart';

part 'user.freezed.dart';
part 'user.g.dart';

/// Mirrors the API's `UserDto`.
@freezed
abstract class User with _$User {
  const factory User({
    required String id,
    required String email,
    required String role,
    String? employeeId,
    String? firstName,
    String? lastName,
    DateTime? lastLoginAt,
  }) = _User;

  const User._();

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);

  /// Full name when the account is linked to an employee, e-mail otherwise.
  String get displayName {
    final first = firstName?.trim() ?? '';
    final last = lastName?.trim() ?? '';
    final full = '$first $last'.trim();
    return full.isEmpty ? email : full;
  }

  /// Only accounts linked to an employee may report GPS positions.
  bool get isEmployee => employeeId != null;

  /// Roles the API lets read the employee and project directories.
  static const _directoryRoles = <String>{
    'SuperAdmin',
    'Admin',
    'ProjectManager',
    'Foreman',
  };

  /// Workers are not served the directory endpoints, so the app does not
  /// offer them either.
  bool get canViewDirectory => _directoryRoles.contains(role);

  /// Mirrors the backend's `AdminAndAbove` policy — create/edit/delete on
  /// Vehicles, Tools and Employees.
  static const _adminRoles = <String>{'SuperAdmin', 'Admin'};

  bool get isAdminAndAbove => _adminRoles.contains(role);

  /// Mirrors the backend's `ProjectManagerAndAbove` policy — create/edit on
  /// Projects and Materials (their *delete* is `AdminAndAbove`, stricter —
  /// use [isAdminAndAbove] for that).
  static const _pmRoles = <String>{'SuperAdmin', 'Admin', 'ProjectManager'};

  bool get isProjectManagerAndAbove => _pmRoles.contains(role);

  /// Mirrors the backend's `SuperAdminOnly` policy — the company profile and
  /// the free-form ledger. Not even Admin may reach these.
  bool get isSuperAdmin => role == 'SuperAdmin';
}
