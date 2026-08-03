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
    required DateTime createdAt,
  }) = _Absence;

  const Absence._();

  DateTime? get start => DateTime.tryParse(startDate);

  DateTime? get end => DateTime.tryParse(endDate);

  /// Nobody has answered it yet.
  bool get isPending => status == 'Requested';

  bool get isApproved => status == 'Approved';

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
