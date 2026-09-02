import 'package:freezed_annotation/freezed_annotation.dart';

part 'absence.freezed.dart';
part 'absence.g.dart';

/// Mirrors the API's `AbsenceDto`.
@freezed
abstract class Absence with _$Absence {
  const factory Absence({
    required String id,
    required String employeeId,
    required String employeeName,
    required String type,
    required String status,

    /// `YYYY-MM-DD`.
    required String startDate,

    /// `YYYY-MM-DD`, inclusive.
    required String endDate,

    /// Calendar days covered, both ends included.
    required int dayCount,
    String? reason,
    String? requestedByName,
    String? reviewedByName,
    DateTime? reviewedAt,
    String? reviewNote,

    /// `YYYY-MM-DD`. Set together with [proposedEndDate] while a change to
    /// this (already-approved) absence awaits confirmation from the other
    /// side.
    String? proposedStartDate,

    /// `YYYY-MM-DD`, inclusive.
    String? proposedEndDate,
    String? proposedReason,
    String? proposedByName,

    /// True when the employee proposed the change (so management must
    /// confirm it); false when management proposed it (so the employee
    /// must).
    @Default(false) bool proposedByEmployee,
    DateTime? proposedAt,
    required DateTime createdAt,
  }) = _Absence;

  const Absence._();

  DateTime? get start => DateTime.tryParse(startDate);

  DateTime? get end => DateTime.tryParse(endDate);

  DateTime? get proposedStart =>
      proposedStartDate == null ? null : DateTime.tryParse(proposedStartDate!);

  DateTime? get proposedEnd =>
      proposedEndDate == null ? null : DateTime.tryParse(proposedEndDate!);

  /// Nobody has answered it yet.
  bool get isPending => status == 'Requested';

  bool get isApproved => status == 'Approved';

  /// A change is staged and waiting on the other side to confirm or decline it.
  bool get hasPendingEdit => proposedStartDate != null;

  /// Whether the employee may take this one back.
  ///
  /// Only an unanswered request. Granted leave has to be refused by a
  /// supervisor instead — withdrawing it would leave work planned around days
  /// somebody is still away. The API enforces this; the flag exists so the
  /// screen does not offer a button that can only fail.
  bool get canWithdraw => isPending;

  factory Absence.fromJson(Map<String, dynamic> json) =>
      _$AbsenceFromJson(json);
}
