import 'package:freezed_annotation/freezed_annotation.dart';

part 'vehicle.freezed.dart';
part 'vehicle.g.dart';

/// Mirrors the API's `VehicleDto`, used for both the list and the detail
/// screen — the API serves the same shape from both endpoints.
@freezed
abstract class Vehicle with _$Vehicle {
  const factory Vehicle({
    required String id,
    required String brand,
    required String model,
    required String registrationNumber,
    String? vin,
    required String fuelType,
    required String status,
    String? assignedEmployeeId,
    String? assignedEmployeeName,
    String? assignedEmployeeNumber,
    required DateTime createdAt,
    DateTime? updatedAt,
  }) = _Vehicle;

  const Vehicle._();

  factory Vehicle.fromJson(Map<String, dynamic> json) =>
      _$VehicleFromJson(json);

  String get displayName => '$brand $model';

  bool get isAssigned => assignedEmployeeId != null;
}
