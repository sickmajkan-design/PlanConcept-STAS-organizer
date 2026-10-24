import 'package:freezed_annotation/freezed_annotation.dart';

part 'weekly_site_report.freezed.dart';
part 'weekly_site_report.g.dart';

/// Mirrors the API's `WeeklySiteReportDto`.
@freezed
abstract class WeeklySiteReport with _$WeeklySiteReport {
  const factory WeeklySiteReport({
    required String id,
    required String projectId,
    required String projectName,
    required String submittedByEmployeeId,
    required String submittedByEmployeeName,
    required int isoYear,
    required int isoWeek,
    required String type,
    double? quantity,
    String? note,
    required String fileName,
    /// How many pictures the report has, and how many workers are listed in it.
    @Default(1) int pageCount,
    @Default(0) int lineCount,
    /// `App` when the foreman sent it, `Office` when the office entered it.
    @Default('App') String source,
    required String status,
    DateTime? processedAt,
    String? processedByEmail,
    /// Why the office sent the report back, while it waits for a new one.
    String? reviewNote,
    required DateTime createdAt,
  }) = _WeeklySiteReport;

  factory WeeklySiteReport.fromJson(Map<String, dynamic> json) =>
      _$WeeklySiteReportFromJson(json);
}

/// Mirrors the API's `ReportableProjectDto` — a site the caller may file a
/// weekly report for.
@freezed
abstract class ReportableProject with _$ReportableProject {
  const factory ReportableProject({
    required String id,
    required String name,
  }) = _ReportableProject;

  factory ReportableProject.fromJson(Map<String, dynamic> json) =>
      _$ReportableProjectFromJson(json);
}
