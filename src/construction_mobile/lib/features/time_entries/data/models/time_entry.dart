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

    /// Set when the nightly sweep force-closed this shift instead of the
    /// worker clocking out themselves.
    @Default(false) bool autoClosed,

    /// Null when the project (or the clock-in point) has no location to
    /// compare against. Otherwise whether the clock-in was close enough to
    /// the project's own coordinates.
    bool? locationCorrect,

    /// Null when the project has no expected shift start time set.
    /// Otherwise whether the clock-in landed close enough to it.
    bool? timeCorrect,
  }) = _TimeEntry;

  const TimeEntry._();

  factory TimeEntry.fromJson(Map<String, dynamic> json) =>
      _$TimeEntryFromJson(json);

  bool get isRunning => endedAt == null;

  /// An approved entry is payroll evidence; the API refuses to change it.
  bool get isLocked => status == 'Approved';

  /// Worth a flag in the shift-history list: auto-closed, or clocked in
  /// somewhere/somewhen the project didn't expect.
  bool get needsAttention =>
      autoClosed || locationCorrect == false || timeCorrect == false;

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
