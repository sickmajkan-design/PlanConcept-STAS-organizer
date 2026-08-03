import 'package:freezed_annotation/freezed_annotation.dart';

part 'schedule.freezed.dart';
part 'schedule.g.dart';

/// Mirrors the API's `ScheduleDto`, narrowed by the server to the caller's own
/// line when they are a worker.
@freezed
abstract class Schedule with _$Schedule {
  const factory Schedule({
    required String from,
    required String to,
    @Default(<ScheduleRow>[]) List<ScheduleRow> rows,
  }) = _Schedule;

  const Schedule._();

  /// The signed-in employee's line, when the server sent exactly one.
  ScheduleRow? get mine => rows.length == 1 ? rows.first : null;

  factory Schedule.fromJson(Map<String, dynamic> json) =>
      _$ScheduleFromJson(json);
}

@freezed
abstract class ScheduleRow with _$ScheduleRow {
  const factory ScheduleRow({
    required String employeeId,
    required String employeeName,
    required String position,
    @Default(<ScheduleAssignment>[]) List<ScheduleAssignment> assignments,

    /// Granted leave only. A request nobody has answered is not on the board.
    @Default(<ScheduleAbsence>[]) List<ScheduleAbsence> absences,
  }) = _ScheduleRow;

  const ScheduleRow._();

  bool get isEmpty => assignments.isEmpty && absences.isEmpty;

  factory ScheduleRow.fromJson(Map<String, dynamic> json) =>
      _$ScheduleRowFromJson(json);
}

/// A posting, already clipped by the API to the window that was asked for.
@freezed
abstract class ScheduleAssignment with _$ScheduleAssignment {
  const factory ScheduleAssignment({
    required String id,
    required String projectId,
    required String projectName,
    required String from,
    required String to,

    /// True when the posting runs on past the end of the window.
    @Default(false) bool continuesAfter,
  }) = _ScheduleAssignment;

  const ScheduleAssignment._();

  DateTime? get start => DateTime.tryParse(from);

  DateTime? get end => DateTime.tryParse(to);

  factory ScheduleAssignment.fromJson(Map<String, dynamic> json) =>
      _$ScheduleAssignmentFromJson(json);
}

@freezed
abstract class ScheduleAbsence with _$ScheduleAbsence {
  const factory ScheduleAbsence({
    required String id,
    required String type,
    required String from,
    required String to,
  }) = _ScheduleAbsence;

  const ScheduleAbsence._();

  DateTime? get start => DateTime.tryParse(from);

  DateTime? get end => DateTime.tryParse(to);

  factory ScheduleAbsence.fromJson(Map<String, dynamic> json) =>
      _$ScheduleAbsenceFromJson(json);
}
