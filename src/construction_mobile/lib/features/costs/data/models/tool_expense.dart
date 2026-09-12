import 'package:freezed_annotation/freezed_annotation.dart';

part 'tool_expense.freezed.dart';
part 'tool_expense.g.dart';

/// Mirrors the API's `ToolExpenseDto` — see `VehicleExpense` for the pattern
/// this mirrors. Simpler than a vehicle's: no fuel, so no litres/price-per-
/// litre/odometer fields exist here at all.
@freezed
abstract class ToolExpense with _$ToolExpense {
  const factory ToolExpense({
    required String id,
    required String toolId,
    required String toolName,
    required String kind,
    required double amount,

    /// `YYYY-MM-DD`.
    required String occurredOn,
    String? supplier,
    String? note,
    String? recordedByName,
    required DateTime createdAt,
  }) = _ToolExpense;

  const ToolExpense._();

  DateTime? get occurred => DateTime.tryParse(occurredOn);

  factory ToolExpense.fromJson(Map<String, dynamic> json) =>
      _$ToolExpenseFromJson(json);
}
