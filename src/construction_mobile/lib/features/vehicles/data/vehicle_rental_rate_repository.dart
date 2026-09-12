import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/models/paged_list.dart';
import '../../../core/network/api_repository.dart';
import '../../../core/network/network_providers.dart';
import 'models/vehicle_rental_rate.dart';

/// The rent/lease rate history for a vehicle — separate from the vehicle's
/// own `ownershipType` flag, which only says whether it is rented at all.
/// Setting a new rate here is what actually puts a rental in force.
class VehicleRentalRateRepository extends ApiRepository {
  const VehicleRentalRateRepository(super.dio);

  Future<PagedList<VehicleRentalRate>> fetch({
    required String vehicleId,
    int pageNumber = 1,
    int pageSize = 20,
  }) {
    return getPaged(
      '/api/v1/vehicle-rental-rates',
      VehicleRentalRate.fromJson,
      query: pagedQuery(
        pageNumber: pageNumber,
        pageSize: pageSize,
        sortBy: 'startDate',
        sortDescending: true,
        filters: {'vehicleId': vehicleId},
      ),
    );
  }

  /// Puts a new rate in force, closing off whatever open-ended rate was
  /// running before it.
  Future<VehicleRentalRate> set({
    required String vehicleId,
    required double monthlyAmount,
    String? provider,
    DateTime? startDate,
    DateTime? endDate,
    String? note,
    String? idempotencyKey,
  }) {
    return postJson(
      '/api/v1/vehicle-rental-rates',
      VehicleRentalRate.fromJson,
      idempotencyKey: idempotencyKey,
      data: {
        'vehicleId': vehicleId,
        'monthlyAmount': monthlyAmount,
        'provider': ?provider,
        'startDate': ?startDate?.toIso8601String(),
        'endDate': ?endDate?.toIso8601String(),
        'note': ?note,
      },
    );
  }

  /// Corrects a mistake on an existing rate's own fields — never chains into
  /// a neighbouring rate the way `set` does.
  Future<VehicleRentalRate> update(
    String id, {
    required double monthlyAmount,
    String? provider,
    required DateTime startDate,
    DateTime? endDate,
    String? note,
  }) {
    return putJson(
      '/api/v1/vehicle-rental-rates/$id',
      VehicleRentalRate.fromJson,
      data: {
        'monthlyAmount': monthlyAmount,
        'provider': ?provider,
        'startDate': startDate.toIso8601String(),
        'endDate': ?endDate?.toIso8601String(),
        'note': ?note,
      },
    );
  }

  Future<void> remove(String id) {
    return deleteVoid('/api/v1/vehicle-rental-rates/$id');
  }
}

final vehicleRentalRateRepositoryProvider =
    Provider<VehicleRentalRateRepository>((ref) {
  return VehicleRentalRateRepository(ref.watch(apiClientProvider));
});
