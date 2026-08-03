import 'package:freezed_annotation/freezed_annotation.dart';

part 'time_entry.freezed.dart';
part 'time_entry.g.dart';

/// Mirrors the API's `TimeEntryDto`.
@freezed
abstract class TimeEntry with _$TimeEntry {
  const factory TimeEntry({
    required String id,
    required String employeeId,
    required String employeeName,
    String? projectId,
    String? projectName,
    required DateTime startedAt,
    DateTime? endedAt,
    required int breakMinutes,

    /// Null while the shift is still running.
    int? workedMinutes,
    required String workType,
    required String status,
    String? note,
    double? startLatitude,
    double? startLongitude,
    double? endLatitude,
    double? endLongitude,
    String? reviewedByName,
    DateTime? reviewedAt,
    String? reviewNote,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _TimeEntry;

  const TimeEntry._();

  factory TimeEntry.fromJson(Map<String, dynamic> json) =>
      _$TimeEntryFromJson(json);

  bool get isRunning => endedAt == null;

  /// An approved entry is payroll evidence; the API refuses to change it.
  bool get isLocked => status == 'Approved';

  /// How long the shift has been going, for the running-shift card.
  ///
  /// Measured against the caller's `now` rather than read from a field so the
  /// number ticks while the screen is open. Clamped at zero because a phone
  /// whose clock is behind the server's would otherwise count backwards.
  Duration elapsedAt(DateTime now) {
    final end = endedAt ?? now.toUtc();
    final elapsed = end.difference(startedAt.toUtc());

    return elapsed.isNegative ? Duration.zero : elapsed;
  }
}
