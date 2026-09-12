import 'package:freezed_annotation/freezed_annotation.dart';

part 'tool_rental_out.freezed.dart';
part 'tool_rental_out.g.dart';

/// Mirrors the API's `ToolRentalOutDto` — see `VehicleRentalOut` for the
/// pattern this mirrors.
@freezed
abstract class ToolRentalOut with _$ToolRentalOut {
  const factory ToolRentalOut({
    required String id,
    required String toolId,
    required String toolName,
    String? customerId,
    required String renterDisplayName,
    required String renterName,
    required double dailyRate,
    required DateTime startDate,
    DateTime? endDate,
    required bool isOpen,
    String? note,
    String? setByName,
    required DateTime createdAt,
  }) = _ToolRentalOut;

  factory ToolRentalOut.fromJson(Map<String, dynamic> json) =>
      _$ToolRentalOutFromJson(json);
}
