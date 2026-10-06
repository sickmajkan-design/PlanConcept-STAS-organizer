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

    /// The company's own number for the vehicle, as on the fuel-card statement. Null on a vehicle created
    /// before it was required.
    String? tdNumber,

    // Sent back unchanged when the vehicle is edited, so saving from here never wipes what the web panel set.
    String? gpsProvider,
    String? gpsTrackingUrl,
    String? branchId,

    /// `YYYY-MM-DD`. The dates a vehicle has to be renewed by.
    String? registrationValidUntil,
    String? rentedUntil,
    String? technicalInspectionValidUntil,
    String? insuranceValidUntil,
    String? nextServiceDue,
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

    /// `YYYY-MM-DD`: when the vehicle that is out is due back, if agreed.
    String? currentRentalOutExpectedEndDate,

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

  /// Rented or leased: the vehicle belongs to somebody else, so a rental end applies.
  bool get isHeldOnRental => ownershipType != 'Owned';

  bool get isLoanedOut => currentRentalOutRenterName != null;
}
