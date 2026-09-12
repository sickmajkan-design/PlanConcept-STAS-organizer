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
    String? qrCode,
    required String fuelType,
    required String status,

    /// `"Owned"` or `"Rented"`.
    @Default('Owned') String ownershipType,

    /// Set when a rental/lease rate is currently in force. Null for an owned
    /// vehicle, or one with no rate on file.
    double? currentRentalMonthlyAmount,
    String? currentRentalProvider,

    /// Set when this vehicle is currently loaned out to another company.
    String? currentRentalOutRenterName,
    double? currentRentalOutDailyRate,

    /// `YYYY-MM-DD`.
    String? currentRentalOutStartDate,

    /// Renter on the most recently closed rental-out loan. Null if never
    /// loaned out.
    String? lastRentalOutRenterName,

    /// `YYYY-MM-DD`.
    String? lastRentalOutEndDate,
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

  bool get isRented => ownershipType == 'Rented';

  bool get isLoanedOut => currentRentalOutRenterName != null;
}
