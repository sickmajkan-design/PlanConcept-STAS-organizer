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
}
