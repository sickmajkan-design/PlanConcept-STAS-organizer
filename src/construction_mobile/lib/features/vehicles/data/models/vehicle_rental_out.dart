import 'package:freezed_annotation/freezed_annotation.dart';

part 'vehicle_rental_out.freezed.dart';
part 'vehicle_rental_out.g.dart';

/// Mirrors the API's `VehicleRentalOutDto` — a loan of this vehicle out to
/// another company or person, the revenue direction opposite
/// `VehicleRentalRate`. `endDate == null` means it has not come back yet.
@freezed
abstract class VehicleRentalOut with _$VehicleRentalOut {
  const factory VehicleRentalOut({
    required String id,
    required String vehicleId,
    required String vehicleName,
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
  }) = _VehicleRentalOut;

  factory VehicleRentalOut.fromJson(Map<String, dynamic> json) =>
      _$VehicleRentalOutFromJson(json);
}
