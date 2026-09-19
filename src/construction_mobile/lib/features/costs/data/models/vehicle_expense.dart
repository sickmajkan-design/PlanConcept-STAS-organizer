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

    /// `Pending`, `Approved` or `Rejected`. Pending while an older API that
    /// predates the review workflow leaves it out.
    @Default('Pending') String status,

    /// Why a reviewer sent it back. Set only when [status] is `Rejected`.
    String? reviewNote,
  }) = _VehicleExpense;

  const VehicleExpense._();

  bool get isFuel => kind == 'Fuel';

  bool get isRejected => status == 'Rejected';

  DateTime? get occurred => DateTime.tryParse(occurredOn);

  factory VehicleExpense.fromJson(Map<String, dynamic> json) =>
      _$VehicleExpenseFromJson(json);
}
