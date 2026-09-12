import 'package:freezed_annotation/freezed_annotation.dart';

part 'tool_rental_rate.freezed.dart';
part 'tool_rental_rate.g.dart';

/// Mirrors the API's `ToolRentalRateDto` — one entry in a tool's rent/lease
/// history. `endDate == null` means this rate is the one currently in force.
@freezed
abstract class ToolRentalRate with _$ToolRentalRate {
  const factory ToolRentalRate({
    required String id,
    required String toolId,
    required String toolName,
    required double monthlyAmount,
    String? provider,
    required DateTime startDate,
    DateTime? endDate,
    String? note,
    String? setByName,
    required DateTime createdAt,
  }) = _ToolRentalRate;

  factory ToolRentalRate.fromJson(Map<String, dynamic> json) =>
      _$ToolRentalRateFromJson(json);
}
