import 'package:freezed_annotation/freezed_annotation.dart';

part 'project.freezed.dart';
part 'project.g.dart';

/// Mirrors the API's `ProjectDto`.
@freezed
abstract class Project with _$Project {
  const factory Project({
    required String id,
    required String name,
    String? description,
    String? customerId,
    String? customerName,
    String? parentProjectId,
    String? parentProjectName,

    /// `"Main"` or `"Sub"`, derived server-side from whether the project has
    /// a parent — never stored, never chosen by the caller.
    @Default('Main') String kind,
    @Default(0) int subProjectCount,
    String? address,
    double? latitude,
    double? longitude,
    String? countryCode,
    String? shiftStartTime,
    DateTime? startDate,
    DateTime? endDate,
    required String status,
    double? contractValue,
    @Default(0) int employeeCount,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _Project;

  const Project._();

  factory Project.fromJson(Map<String, dynamic> json) =>
      _$ProjectFromJson(json);

  /// Both coordinates are always stored together, so one check is enough.
  bool get hasCoordinates => latitude != null && longitude != null;

  bool get isSubProject => kind == 'Sub';
}

/// Mirrors the API's `ProjectDetailDto` — the list fields plus the crew.
@freezed
abstract class ProjectDetail with _$ProjectDetail {
  const factory ProjectDetail({
    required String id,
    required String name,
    String? description,
    String? customerId,
    String? customerName,
    String? parentProjectId,
    String? parentProjectName,
    @Default('Main') String kind,
    @Default(0) int subProjectCount,
    String? address,
    double? latitude,
    double? longitude,
    String? countryCode,
    String? shiftStartTime,
    DateTime? startDate,
    DateTime? endDate,
    required String status,
    double? contractValue,
    @Default(0) int employeeCount,
    required DateTime createdAt,
    DateTime? updatedAt,
    @Default(<ProjectEmployee>[]) List<ProjectEmployee> employees,
  }) = _ProjectDetail;

  const ProjectDetail._();

  factory ProjectDetail.fromJson(Map<String, dynamic> json) =>
      _$ProjectDetailFromJson(json);

  bool get hasCoordinates => latitude != null && longitude != null;

  bool get isSubProject => kind == 'Sub';
}

@freezed
abstract class ProjectEmployee with _$ProjectEmployee {
  const factory ProjectEmployee({
    required String employeeId,
    required String employeeNumber,
    required String fullName,
    required String position,
    required String status,
    required DateTime assignedAt,
  }) = _ProjectEmployee;

  factory ProjectEmployee.fromJson(Map<String, dynamic> json) =>
      _$ProjectEmployeeFromJson(json);
}
