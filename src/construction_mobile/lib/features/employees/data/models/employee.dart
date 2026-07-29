import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/formatting.dart';

part 'employee.freezed.dart';
part 'employee.g.dart';

/// Mirrors the API's `EmployeeDto`.
@freezed
abstract class Employee with _$Employee {
  const factory Employee({
    required String id,
    required String employeeNumber,
    required String firstName,
    required String lastName,
    required String fullName,
    String? phone,
    String? email,
    String? address,
    DateTime? dateOfBirth,
    required DateTime employmentDate,
    required String position,
    required String status,
    String? photoUrl,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _Employee;

  const Employee._();

  factory Employee.fromJson(Map<String, dynamic> json) =>
      _$EmployeeFromJson(json);

  String get initials => initialsOf(firstName, lastName);
}

/// Mirrors the API's `EmployeeDetailDto` — the list fields plus the
/// employee's project assignments.
@freezed
abstract class EmployeeDetail with _$EmployeeDetail {
  const factory EmployeeDetail({
    required String id,
    required String employeeNumber,
    required String firstName,
    required String lastName,
    required String fullName,
    String? phone,
    String? email,
    String? address,
    DateTime? dateOfBirth,
    required DateTime employmentDate,
    required String position,
    required String status,
    String? photoUrl,
    required DateTime createdAt,
    DateTime? updatedAt,
    @Default(false) bool hasUserAccount,
    @Default(<EmployeeProjectAssignment>[])
    List<EmployeeProjectAssignment> projects,
  }) = _EmployeeDetail;

  const EmployeeDetail._();

  factory EmployeeDetail.fromJson(Map<String, dynamic> json) =>
      _$EmployeeDetailFromJson(json);

  String get initials => initialsOf(firstName, lastName);
}

@freezed
abstract class EmployeeProjectAssignment with _$EmployeeProjectAssignment {
  const factory EmployeeProjectAssignment({
    required String projectId,
    required String projectName,
    required String projectStatus,
    required DateTime assignedAt,
  }) = _EmployeeProjectAssignment;

  factory EmployeeProjectAssignment.fromJson(Map<String, dynamic> json) =>
      _$EmployeeProjectAssignmentFromJson(json);
}
