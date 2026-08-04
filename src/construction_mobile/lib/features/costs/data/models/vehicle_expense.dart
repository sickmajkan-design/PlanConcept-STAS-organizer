import 'package:freezed_annotation/freezed_annotation.dart';

part 'vehicle_expense.freezed.dart';
part 'vehicle_expense.g.dart';

/// Mirrors the API's `VehicleExpenseDto`.
@freezed
abstract class VehicleExpense with _$VehicleExpense {
  const factory VehicleExpense({
    required String id,
    required String vehicleId,
    required String vehicleName,
    required String kind,
    required double amount,

    /// `YYYY-MM-DD`.
    required String occurredOn,

    /// Only ever set on a fill-up.
    double? litres,
    double? pricePerLitre,
    int? odometerKm,
    String? supplier,
    String? note,
    String? recordedByName,
    required DateTime createdAt,
  }) = _VehicleExpense;

  const VehicleExpense._();

  bool get isFuel => kind == 'Fuel';

  DateTime? get occurred => DateTime.tryParse(occurredOn);

  factory VehicleExpense.fromJson(Map<String, dynamic> json) =>
      _$VehicleExpenseFromJson(json);
}
