import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/vehicle_rental_out.dart';

/// Loans of a vehicle out to another company or person — the revenue
/// direction, opposite `VehicleRentalRateRepository`.
class VehicleRentalOutRepository extends ApiRepository {
  const VehicleRentalOutRepository(super.dio);

  Future<PagedList<VehicleRentalOut>> fetch({
    required String vehicleId,
    int pageNumber = 1,
    int pageSize = 20,
  }) {
    return getPaged(
      '/api/v1/vehicle-rentals-out',
      VehicleRentalOut.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'startDate',
        sortDescending: true,
        filters: {'vehicleId': vehicleId},
      ),
    );
  }

  /// Records the vehicle going out. Requires it to currently be `Available`
  /// — the API refuses otherwise.
  Future<VehicleRentalOut> record({
    required String vehicleId,
    String? customerId,
    required String renterName,
    required double dailyRate,
    DateTime? startDate,
    String? note,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/vehicle-rentals-out',
      VehicleRentalOut.fromJson,
      idempotencyKey: idempotencyKey,
      data: {
        'vehicleId': vehicleId,
        'customerId': ?customerId,
        'renterName': renterName,
        'dailyRate': dailyRate,
        'startDate': ?startDate?.toIso8601String(),
        'note': ?note,
      },
    );
  }

  /// Closes an open loan and frees the vehicle back to `Available`.
  Future<VehicleRentalOut> returnRental(
    String id, {
    DateTime? endDate,
    String? idempotencyKey,
  }) {
    return putJson(
      '/api/v1/vehicle-rentals-out/$id/return',
      VehicleRentalOut.fromJson,
      data: {'endDate': ?endDate?.toIso8601String()},
    );
  }

  /// Corrects a mistake on the loan's own fields — never touches whether it
  /// has been returned.
  Future<VehicleRentalOut> update(
    String id, {
    String? customerId,
    required String renterName,
    required double dailyRate,
    required DateTime startDate,
    String? note,
  }) {
    return putJson(
      '/api/v1/vehicle-rentals-out/$id',
      VehicleRentalOut.fromJson,
      data: {
        'customerId': ?customerId,
        'renterName': renterName,
        'dailyRate': dailyRate,
        'startDate': startDate.toIso8601String(),
        'note': ?note,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/vehicle-rentals-out/$id');
  }
}

final vehicleRentalOutRepositoryProvider =
    Provider<VehicleRentalOutRepository>((ref) {
  return VehicleRentalOutRepository(ref.watch(apiClientProvider));
});
