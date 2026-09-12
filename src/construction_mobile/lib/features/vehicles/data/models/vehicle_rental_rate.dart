import 'package:freezed_annotation/freezed_annotation.dart';

part 'vehicle_rental_rate.freezed.dart';
part 'vehicle_rental_rate.g.dart';

/// Mirrors the API's `VehicleRentalRateDto` — one entry in a vehicle's
/// rent/lease history. `endDate == null` means this rate is the one
/// currently in force.
@freezed
abstract class VehicleRentalRate with _$VehicleRentalRate {
  const factory VehicleRentalRate({
    required String id,
    required String vehicleId,
    required String vehicleName,
    required double monthlyAmount,
    String? provider,
    required DateTime startDate,
    DateTime? endDate,
    String? note,
    String? setByName,
    required DateTime createdAt,
  }) = _VehicleRentalRate;

  factory VehicleRentalRate.fromJson(Map<String, dynamic> json) =>
      _$VehicleRentalRateFromJson(json);
}
