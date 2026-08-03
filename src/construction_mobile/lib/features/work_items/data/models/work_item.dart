import 'package:freezed_annotation/freezed_annotation.dart';

part 'work_item.freezed.dart';
part 'work_item.g.dart';

/// Mirrors the API's `WorkItemDto`.
@freezed
abstract class WorkItem with _$WorkItem {
  const factory WorkItem({
    required String id,
    required String kind,
    required String title,
    String? description,
    String? projectId,
    String? projectName,
    String? assignedEmployeeId,
    String? assignedEmployeeName,
    required String priority,
    required String status,

    /// `YYYY-MM-DD`, or null when nobody set a deadline.
    String? dueDate,
    double? latitude,
    double? longitude,
    String? createdByName,
    String? resolvedByName,
    DateTime? resolvedAt,
    @Default(0) int attachmentCount,
    @Default(false) bool isFinished,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _WorkItem;

  const WorkItem._();

  bool get isDefect => kind == 'Defect';

  DateTime? get due => dueDate == null ? null : DateTime.tryParse(dueDate!);

  /// Past its deadline and still to do.
  ///
  /// Compared date-only: work due "on the 3rd" is not late during the 3rd, and
  /// comparing instants would mark it overdue at midnight.
  bool isOverdueOn(DateTime today) {
    final deadline = due;

    if (deadline == null || isFinished) {
      return false;
    }

    return DateTime(deadline.year, deadline.month, deadline.day)
        .isBefore(DateTime(today.year, today.month, today.day));
  }

  /// The states this one may move to, mirroring the API's transition table.
  ///
  /// Held here so the app offers only moves the server will accept; a button
  /// that produces a 409 teaches the user nothing the screen could not have
  /// shown. The API remains the authority.
  List<String> get nextStates => switch (status) {
        'Open' => const ['InProgress', 'Resolved'],
        'InProgress' => const ['Resolved', 'Open'],
        // Closing is a supervisor's call, so the phone does not offer it.
        'Resolved' => const ['InProgress'],
        _ => const [],
      };

  factory WorkItem.fromJson(Map<String, dynamic> json) =>
      _$WorkItemFromJson(json);
}
