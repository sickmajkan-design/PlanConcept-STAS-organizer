import 'package:freezed_annotation/freezed_annotation.dart';

part 'vehicle_toll.freezed.dart';
part 'vehicle_toll.g.dart';

/// Mirrors the API's `VehicleTollDto` — one vignette, tunnel toll or road
/// passage charge carried by a vehicle. Read-only on mobile.
///
/// Show [computedState] (`Unpaid` / `Paid` / `ExpiringSoon` / `Expired`), not
/// [status]: the server derives it from the paid-until date, so a "Paid" toll
/// that has lapsed already reads `Expired`. [validUntil] is a date-only value.
@freezed
abstract class VehicleToll with _$VehicleToll {
  const factory VehicleToll({
    required String id,
    required String vehicleId,
    required String type,
    required String country,
    String? routeSegment,
    required String status,
    DateTime? validUntil,
    required String computedState,
    String? paidByUserName,
    DateTime? paidAt,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _VehicleToll;

  factory VehicleToll.fromJson(Map<String, dynamic> json) =>
      _$VehicleTollFromJson(json);
}
